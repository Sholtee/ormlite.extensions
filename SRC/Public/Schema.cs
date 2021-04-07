/********************************************************************************
* Schema.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions
{
    using Internals;
    using Primitives;
    using Properties;
    
    /// <summary>
    /// Represents a database schema.
    /// </summary>
    [SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
    public class Schema
    {
        /// <summary>
        /// The <see cref="IDbConnection"/> to access the database.
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Tables contained by this <see cref="Schema"/>.
        /// </summary>
        public ICollection<Type> Tables { get; }

        /// <summary>
        /// Creates a new <see cref="Schema"/> instance.
        /// </summary>
        public Schema(IDbConnection connection, params Type[] tables)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (tables is null)
                throw new ArgumentNullException(nameof(tables));

            Tables = NodeUtils.Flatten(tables);
        }

        /// <summary>
        /// Creates a new <see cref="Schema"/> instance.
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="asmsToSearch">Assemblies that contain the data table definitions.</param>
        public Schema(IDbConnection connection, params Assembly[] asmsToSearch): this(connection, asmsToSearch
            .SelectMany(asm => asm.GetTypes())
            .Where(type => type.GetCustomAttribute<DataTableAttribute>(inherit: false) is not null)
            .ToArray())
        {
        }

        /// <summary>
        /// Creates the tables in the schema.
        /// </summary>
        public void CreateTablesCascaded() 
        {
            IOrmLiteDialectProvider dialectProvider = Connection.GetDialectProvider();

            //
            // Ezt nem lehet kotegelten lekerdezni mivel az IDialectProvider-ben nincs 
            // "ToTableExistsStatement" v hasonlo
            //

            IEnumerable<Type> tablesToCreate = Tables.Where(table => !Connection.TableExists
            (
                dialectProvider.NamingStrategy.GetTableName(table.GetModelMetadata()))
            );

            using IBulkedDbConnection bulk = Connection.CreateBulkedDbConnection();

            foreach (Type table in tablesToCreate) 
            {
                bulk.ExecuteNonQuery(dialectProvider.ToCreateTableStatement(table));

                var setters = table
                    .GetModelMetadata()
                    .FieldDefinitions
                    .Select(def => new
                    {
                        Fn = def.PropertyInfo.ToSetter(),
                        def.FieldType
                    })
                    .ToArray();

                Func<object?[], object> insert = ((Func<object, bool, long>) bulk.Insert)
                    .Method
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(table)
                    .ToStaticDelegate();

                foreach (ValuesAttribute values in table.GetCustomAttributes<ValuesAttribute>())
                {
                    if (values.Items.Count != setters.Length)
                        throw new InvalidOperationException(Resources.VALUE_COUNT_NOT_MATCH);

                    object inst = Activator.CreateInstance(table)!;

                    for (int i = 0; i < setters.Length; i++)
                    {
                        object? val = values.Items[i];
                        var setter = setters[i];

                        setter.Fn(inst, val is null || val.GetType() == setter.FieldType
                            ? val
                            : TypeDescriptor.GetConverter(setter.FieldType).ConvertFrom(val));
                    }

                    insert(new object?[] { bulk, inst, false });
                }
            }

            bulk.Flush();
        }

        /// <summary>
        /// Drops the tables in the schema
        /// </summary>
        public void DropTablesCascaded() =>
            //
            // Itt semmit nem lehet kotegelten vegrehajtani mivel az IDialectProvider-ben nincs
            // sem "ToTableExistsStatement" sem "ToDropTableStatement()" v hasonlo. 
            // 

            Connection.DropTables(Tables.Reverse().ToArray());
    }
}

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

using ServiceStack.DataAnnotations;
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
        internal const string INITIAL_COMMIT = nameof(INITIAL_COMMIT);

        private sealed class Migration 
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [Required]
            public DateTime InstalledAtUtc { get; set; }

            [Required]
            #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public string Sql { get; set; }

            [Index(Unique = true)]
            public string Name { get; set; }
            #pragma warning restore CS8618
        }

        /// <summary>
        /// The <see cref="IDbConnection"/> to access the database.
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Table definitions in this schema.
        /// </summary>
        public IReadOnlyList<Type> Tables { get; }

        /// <summary>
        /// Creates a new <see cref="Schema"/> instance.
        /// </summary>
        public Schema(IDbConnection connection, params Type[] tables)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Tables = NodeUtils
                .Flatten(tables ?? throw new ArgumentNullException(nameof(tables)))
                .Append(typeof(Migration))
                .ToArray();
        }

        /// <summary>
        /// Creates a new <see cref="Schema"/> instance.
        /// </summary>
        public Schema(IDbConnection connection, params Assembly[] asmsToSearch) : this(connection, asmsToSearch
            .SelectMany(asm => asm.GetTypes())
            .Where(type => type.GetCustomAttribute<DataTableAttribute>(inherit: false) is not null)
            .ToArray()) 
        {
        } 

        /// <summary>
        /// Initializes the schema.
        /// </summary>
        public void Initialize() 
        {
            if (IsInitialized)
                throw new InvalidOperationException(Resources.ALREADY_INITIALIZED);

            IOrmLiteDialectProvider dialectProvider = Connection.GetDialectProvider();

            using IBulkedDbConnection bulk = Connection.CreateBulkedDbConnection();

            foreach (Type table in Tables)
            {
                bulk.ExecuteNonQuery(dialectProvider.ToCreateTableStatement(table));

                ValuesAttribute[] initialValues = table.GetCustomAttributes<ValuesAttribute>().ToArray();
                if (initialValues.Length == 0)
                    continue;

                var setters = table
                    .GetModelMetadata()
                    .FieldDefinitions
                    .Select(def => new
                    {
                        Fn = def.PropertyInfo.ToSetter(),
                        def.FieldType
                    })
                    .ToArray();

                Func<object?[], object> insert = ((Func<object, bool, bool, long>) bulk.Insert)
                    .Method
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(table)
                    .ToStaticDelegate();

                Func<object?[], object> factory = (table.GetConstructor(Type.EmptyTypes) ?? throw new MissingMemberException(table.Name, "ctor"))
                    .ToStaticDelegate();

                foreach (ValuesAttribute values in initialValues)
                {
                    if (values.Items.Count != setters.Length)
                        throw new InvalidOperationException(Resources.VALUE_COUNT_NOT_MATCH);

                    object inst = factory(Array.Empty<object?>());

                    for (int i = 0; i < setters.Length; i++)
                    {
                        object? val = values.Items[i];
                        var setter = setters[i];

                        setter.Fn(inst, val is null || val.GetType() == setter.FieldType
                            ? val
                            : TypeDescriptor.GetConverter(setter.FieldType).ConvertFrom(val));
                    }

                    insert(new object?[] { bulk, inst, false, false });
                }
            }

            bulk.Insert(new Migration
            {
                Name = INITIAL_COMMIT,
                InstalledAtUtc = DateTime.UtcNow,
                Sql = bulk.ToString()
            });

            //
            // Itt felesleges tranzakciot inditani, mivel a DDL operaciok nem tranzakciozhatok (MySQL-ben biztosan):
            // https://dev.mysql.com/doc/refman/8.0/en/cannot-roll-back.html
            //

            bulk.Flush();
        }

        /// <summary>
        /// Returns true if the schema has already been initialized.
        /// </summary>
        public bool IsInitialized => Connection.TableExists<Migration>() && Connection.Exists<Migration>(m => m.Name == INITIAL_COMMIT);

        /// <summary>
        /// Executes a named migration script.
        /// </summary>
        public bool Migrate(string name, string sql)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (sql is null)
                throw new ArgumentNullException(nameof(sql));

            if (Connection.Exists<Migration>(mig => mig.Name == name))
                return false;

            Connection.ExecuteNonQuery(sql);

            Connection.Insert(new Migration
            {
                Name = name,
                InstalledAtUtc = DateTime.UtcNow,
                Sql = sql
            });

            return true;
        }

        /// <summary>
        /// Gets the name of the last migration script.
        /// </summary>
        public string GetLastMigration()
        {
            string sql = Connection.From<Migration>()
                .Select(m => m.Name)
                .OrderByDescending(m => m.InstalledAtUtc)
                .Limit(1)
                .ToMergedParamsSelectStatement();
            return Connection.Scalar<string>(sql);
        }

        /// <summary>
        /// Drops the schema.
        /// </summary>
        public void Drop() =>
            //
            // Itt semmit nem lehet kotegelten vegrehajtani mivel az IDialectProvider-ben nincs
            // sem "ToTableExistsStatement" sem "ToDropTableStatement()" v hasonlo. 
            // 

            Connection.DropTables
            (
                Tables.Reverse().ToArray()
            );
    }
}

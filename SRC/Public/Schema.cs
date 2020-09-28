/********************************************************************************
* Schema.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using ServiceStack.OrmLite;

namespace Solti.Utils.OrmLite.Extensions
{
    using Internals;
    
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

            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            Tables = NodeUtils.Flatten
            (
                tables
                    .Where(type => type.GetCustomAttribute<DataTableAttribute>(inherit: false) != null)
                    .ToArray()
            );
        }

        /// <summary>
        /// Creates a new <see cref="Schema"/> instance.
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="asmsToSearch">Assemblies that contain the data table definitions.</param>
        public Schema(IDbConnection connection, params Assembly[] asmsToSearch): this(connection, asmsToSearch.SelectMany(asm => asm.GetTypes()).ToArray())
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

            using IBulkedDbConnection connection = Connection.CreateBulkedDbConnection();

            foreach (Type table in tablesToCreate) 
            {
                connection.ExecuteNonQuery(dialectProvider.ToCreateTableStatement(table));
            }

            connection.Flush();
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

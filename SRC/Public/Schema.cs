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
using System.Security.Cryptography;
using System.Text;

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

        private static string GetHash(string str)
        {
            using HashAlgorithm algorithm = SHA256.Create();

            StringBuilder sb = new();
            foreach (byte b in algorithm.ComputeHash(Encoding.UTF8.GetBytes(str)))
                sb.Append(b.ToString("X2"));
            
            return sb.ToString();
        }

        private sealed class Migration 
        {
            [PrimaryKey, AutoId]
            public Guid Id { get; set; }

            [Required, Index(Unique = true)]
            public long CreatedAtUtc { get; set; }

            [Required, StringLength(StringLengthAttribute.MaxText)]
            #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public string Sql { get; set; }

            [Required, Index(Unique = true)]
            public string Hash { get; set; }

            public string? Comment { get; set; }
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
            .SelectMany(static asm => asm.GetTypes())
            .Where(static type => type.GetCustomAttribute<DataTableAttribute>(inherit: false) is not null)
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
                if (initialValues.Length is 0)
                    continue;

                var setters = table
                    .GetModelMetadata()
                    .FieldDefinitions
                    .Select(static def => new
                    {
                        Fn = def.PropertyInfo.ToSetter(),
                        def.FieldType
                    })
                    .ToArray();

                StaticMethod insert = ((Func<object, bool, bool, long>) bulk.Insert)
                    .Method
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(table)
                    .ToStaticDelegate();

                StaticMethod factory = (table.GetConstructor(Type.EmptyTypes) ?? throw new MissingMemberException(table.Name, ConstructorInfo.ConstructorName))
                    .ToStaticDelegate();

                foreach (ValuesAttribute values in initialValues)
                {
                    if (values.Items.Count != setters.Length)
                        throw new InvalidOperationException(Resources.VALUE_COUNT_NOT_MATCH);

                    object inst = factory()!;

                    for (int i = 0; i < setters.Length; i++)
                    {
                        object? val = values.Items[i];
                        var setter = setters[i];

                        setter.Fn(inst, val is null || val.GetType() == setter.FieldType
                            ? val
                            : TypeDescriptor.GetConverter(setter.FieldType).ConvertFrom(val));
                    }

                    insert(bulk, inst, false, false);
                }
            }

            string sql = bulk.ToString();

            bulk.Insert(new Migration
            {
                Comment = INITIAL_COMMIT,
                CreatedAtUtc = DateTime.UtcNow.Ticks,
                Sql = sql,
                Hash = GetHash(sql)
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
        public bool IsInitialized => Connection.TableExists<Migration>() && Connection.Exists<Migration>(m => m.Comment == INITIAL_COMMIT);

        /// <summary>
        /// Executes a named migration script.
        /// </summary>
        public bool Migrate(string sql, string? comment = null)
        {
            if (sql is null)
                throw new ArgumentNullException(nameof(sql));

            string hash = GetHash(sql);
            if (Connection.Exists<Migration>(m => m.Hash == hash))
                return false;

            Connection.ExecuteNonQuery(sql);

            Connection.Insert(new Migration
            {
                CreatedAtUtc = DateTime.UtcNow.Ticks,
                Sql = sql,
                Hash = hash,
                Comment = comment
            });

            return true;
        }

        /// <summary>
        /// Gets the name of the last migration script.
        /// </summary>
        public DateTime GetLastMigrationUtc()
        {
            string sql = Connection.From<Migration>()
                .Select(static m => Sql.Max(m.CreatedAtUtc))
                .ToSelectStatement();
            return new DateTime(ticks: Connection.Scalar<long>(sql), DateTimeKind.Utc);
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

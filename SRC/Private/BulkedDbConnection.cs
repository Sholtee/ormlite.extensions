/********************************************************************************
* BulkedDbConnection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

[assembly: InternalsVisibleTo("Solti.Utils.OrmLite.Extensions.Internals.BulkedDbConnection.IDbCommandInterceptor_System.Data.IDbCommand_Proxy")]

namespace Solti.Utils.OrmLite.Extensions.Internals
{
    using Primitives;
    using Proxy;
    using Proxy.Generators;

    internal sealed class BulkedDbConnection: IBulkedDbConnection
    {
        internal IDbConnection Connection { get; }

        internal StringBuilder Buffer { get; }

        public BulkedDbConnection(IDbConnection connection)
        {
            if (connection is BulkedDbConnection) throw new InvalidOperationException(); // TODO
            Connection = connection;

            Buffer = new StringBuilder();
        }

        public void Dispose()
        {
            Buffer.Clear();
        }

        public IDbTransaction BeginTransaction() => throw new NotSupportedException();

        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();

        public void Close()
        {
        }

        public void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is instantiated by the proxy generator")]
        internal class IDbCommandInterceptor : InterfaceInterceptor<IDbCommand>
        {
            private BulkedDbConnection Parent { get; }

            public IDbCommandInterceptor(BulkedDbConnection parent) : base(parent.Connection.CreateCommand()) => 
                Parent = parent;

            private static readonly Regex FCommandTerminated = new Regex(";\\s*$", RegexOptions.Compiled);

            public override object? Invoke(MethodInfo method, object?[] args, MemberInfo extra)
            {
                switch (method.Name)
                {
                    case nameof(Target.ExecuteNonQuery):
                        string command = OrmLiteConfig.DialectProvider.MergeParamsIntoSql(Target!.CommandText, Target
                            .Parameters
                            .Cast<IDbDataParameter>());

                        if (!FCommandTerminated.IsMatch(command)) command += ";";
                        Parent.Buffer.AppendLine(command);

                        return 0;
                    case nameof(Target.ExecuteReader):
                    case nameof(Target.ExecuteScalar):
                        throw new NotSupportedException();
                }

                return base.Invoke(method, args, extra);
            }
        }

        #pragma warning disable CS0618 // Type or member is obsolete
        public IDbCommand CreateCommand() => (IDbCommand) ProxyGenerator<IDbCommand, IDbCommandInterceptor>
            .GeneratedType
        #pragma warning restore CS0618
            .GetConstructor(new[] { typeof(BulkedDbConnection) })
            .ToStaticDelegate()
            .Invoke(new object[] { this });

        public void Open() => throw new NotSupportedException();

        public string ConnectionString
        {
            get => Connection.ConnectionString;
            set => throw new NotSupportedException();
        }

        public int ConnectionTimeout => Connection.ConnectionTimeout;

        public string Database => Connection.Database;

        public ConnectionState State => Connection.State;

        public int Flush()
        {
            if (Buffer.Length == 0) return 0;

            try
            {
                return Connection.ExecuteNonQuery(Buffer.ToString());
            }
            finally
            {
                Buffer.Clear();
            }
        }

        public Task<int> FlushAsync(CancellationToken cancellation) 
        {
            if (Buffer.Length == 0) return Task.FromResult(0);

            try
            {
                return Connection.ExecuteNonQueryAsync(Buffer.ToString(), cancellation);
            }
            finally
            {
                Buffer.Clear();
            }
        }

        public override string ToString() => Buffer.ToString();
    }
}

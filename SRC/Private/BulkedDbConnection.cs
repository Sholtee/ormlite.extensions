/********************************************************************************
* BulkedDbConnection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

using Solti.Utils.Primitives.Patterns;
using Solti.Utils.Primitives.Threading;
using Solti.Utils.Proxy;
using Solti.Utils.Proxy.Attributes;
using Solti.Utils.Proxy.Generators;

[assembly: EmbedGeneratedType(typeof(ProxyGenerator<IDbCommand, Solti.Utils.OrmLite.Extensions.Internals.BulkedDbConnection.IDbCommandInterceptor>))]

namespace Solti.Utils.OrmLite.Extensions.Internals
{
    using Properties;

    internal sealed class BulkedDbConnection: IBulkedDbConnection
    {
        //
        // We cannot inherit from StringBuilder since it is sealed.
        //

        private sealed class PooledStringBuilder : IResettable
        {
            public StringBuilder Instance { get; } = new();

            bool IResettable.Dirty => Instance.Length > 0;

            void IResettable.Reset() => Instance.Clear();
        }

        private static readonly ObjectPool<PooledStringBuilder> FStringBuilderPool = new(static () => new PooledStringBuilder(), Environment.ProcessorCount * 2);

        private readonly IDbConnection FConnection;

        private readonly PooledStringBuilder FPoolItem;

        private readonly StringBuilder FBuffer;

        public BulkedDbConnection(IDbConnection connection)
        {
            if (connection is BulkedDbConnection)
                throw new InvalidOperationException(Resources.ALREADY_PROXIED);

            FConnection = connection;

            FPoolItem = FStringBuilderPool.Get(CheckoutPolicy.Discard) ?? throw new InvalidOperationException(Resources.SB_POOL_EMPTY);
            FBuffer = FPoolItem.Instance;
        }

        public void Dispose() => FStringBuilderPool.Return(FPoolItem);

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

            public IDbCommandInterceptor(BulkedDbConnection parent) : base(parent.FConnection.CreateCommand()) => 
                Parent = parent;

            private static readonly Regex FCommandTerminated = new(";\\s*$", RegexOptions.Compiled);

            public override object? Invoke(InvocationContext context)
            {
                switch (context.Method.Name)
                {
                    case nameof(Target.ExecuteNonQuery):
                        string command = Parent
                            .FConnection
                            .GetDialectProvider()
                            .MergeParamsIntoSql(Target!.CommandText, Target
                                .Parameters
                                .Cast<IDbDataParameter>()
                                .Select(static para => 
                                {
                                    //
                                    // MergeParamsIntoSql() baszik rendesen lekezeni a DBNull-t
                                    //

                                    if (para.Value == DBNull.Value)
                                        para.Value = null;

                                    return para;
                                }));

                        if (!FCommandTerminated.IsMatch(command))
                            command += ";";
                        
                        Parent.FBuffer.AppendLine(command);

                        return 0;
                    case nameof(Target.ExecuteReader):
                    case nameof(Target.ExecuteScalar):
                        throw new NotSupportedException();
                }

                return base.Invoke(context);
            }
        }

        public IDbCommand CreateCommand() => ProxyGenerator<IDbCommand, IDbCommandInterceptor>.Activate(Tuple.Create(this));

        public void Open() => throw new NotSupportedException();

        public string ConnectionString
        {
            get => FConnection.ConnectionString;
            set => throw new NotSupportedException();
        }

        public int ConnectionTimeout => FConnection.ConnectionTimeout;

        public string Database => FConnection.Database;

        public ConnectionState State => FConnection.State;

        public int Flush()
        {
            if (FBuffer.Length == 0)
                return 0;

            try
            {
                return FConnection.ExecuteNonQuery(FBuffer.ToString());
            }
            finally
            {
                FBuffer.Clear();
            }
        }

        public Task<int> FlushAsync(CancellationToken cancellation) 
        {
            if (FBuffer.Length == 0)
                return Task.FromResult(0);

            try
            {
                return FConnection.ExecuteNonQueryAsync(FBuffer.ToString(), cancellation);
            }
            finally
            {
                FBuffer.Clear();
            }
        }

        public override string ToString() => FBuffer.ToString();
    }
}

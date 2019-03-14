using System;
using System.Data;

namespace si.birokrat.next.common.database {
    public interface IConnection {
        IDbConnection SqlConnection { get; }

        bool AutoOpenClose { get; set; }

        int ConnectionTimeout { get; set; }

        int CommandTimeout { get; set; }

        #region // public - control //
        void Open();
        void Close();
        #endregion
        #region // public - generate //
        IDbCommand GenerateCommand();
        IDbDataParameter GenerateParameter(string name, object value);
        IDbDataAdapter GenerateDataAdapter();
        IDbDataAdapter GenerateDataAdapter(IDbCommand command);
        IDbDataAdapter GenerateDataAdapter(string query, IDbConnection connection);
        IDbDataAdapter GenerateDataAdapter(IDbCommand command_delete, IDbCommand command_insert, IDbCommand command_update);
        #endregion
        #region // public - execute //
        DataSet ExecuteDataSet(IDbCommand command);
        DataTable ExecuteDataTable(IDbCommand command);
        bool ExecuteDataTableBoolean(IDbCommand command);
        decimal ExecuteDataTableDecimal(IDbCommand command);
        DateTime ExecuteDataTableDateTime(IDbCommand command);
        Guid ExecuteDataTableGuid(IDbCommand command);
        int ExecuteDataTableInteger(IDbCommand command);
        long ExecuteDataTableLong(IDbCommand command);
        string ExecuteDataTableString(IDbCommand command);
        T ExecuteDataTableType<T>(IDbCommand command);
        int ExecuteNonQuery(IDbCommand command);
        bool ExecuteScalarBoolean(IDbCommand command);
        decimal ExecuteScalarDecimal(IDbCommand command);
        Guid ExecuteScalarGuid(IDbCommand command);
        int ExecuteScalarInteger(IDbCommand command);
        long ExecuteScalarLong(IDbCommand command);
        string ExecuteScalarString(IDbCommand command);
        #endregion
    }
}

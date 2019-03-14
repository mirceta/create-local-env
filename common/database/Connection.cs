using si.birokrat.next.common.conversion;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace si.birokrat.next.common.database {
    public class Connection : IConnection {
        private readonly string _connectionString = string.Empty;

        public Connection(string connectionString) {
            _connectionString = connectionString;
        }

        public IDbConnection SqlConnection { get; private set; } = null;

        public bool AutoOpenClose { get; set; } = true;

        public int CommandTimeout { get; set; } = 60;

        public int ConnectionTimeout { get; set; } = 10;

        #region // public - control //
        public void Open() {
            if (SqlConnection == null) {
                SqlConnection = new SqlConnection(_connectionString);
                SqlConnection.Open();
            }
        }
        public void Close() {
            int maxTimeout = 200;

            if (SqlConnection != null) {
                SqlConnection.Close();
            }

            while (maxTimeout-- > 0) {
                if (SqlConnection.State == ConnectionState.Closed) {
                    break;
                }
                Thread.Sleep(10);
            }
            SqlConnection = null;
        }
        #endregion
        #region // public - generate //
        public IDbCommand GenerateCommand() {
            SqlCommand command = new SqlCommand { CommandTimeout = CommandTimeout };
            return command;
        }
        public IDbDataParameter GenerateParameter(string name, object value) {
            return new SqlParameter(name, value ?? DBNull.Value);
        }
        public IDbDataAdapter GenerateDataAdapter() {
            return new SqlDataAdapter();
        }
        public IDbDataAdapter GenerateDataAdapter(IDbCommand command) {
            return new SqlDataAdapter((SqlCommand)command);
        }
        public IDbDataAdapter GenerateDataAdapter(string query, IDbConnection connection) {
            return new SqlDataAdapter(query, (SqlConnection)connection);
        }
        public IDbDataAdapter GenerateDataAdapter(IDbCommand commandDelete, IDbCommand commandInsert, IDbCommand commandUpdate) {
            SqlDataAdapter adapter = new SqlDataAdapter {
                DeleteCommand = (SqlCommand)commandDelete,
                InsertCommand = (SqlCommand)commandInsert,
                UpdateCommand = (SqlCommand)commandUpdate
            };
            return adapter;
        }
        #endregion
        #region // public - execute //
        public DataSet ExecuteDataSet(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            SqlCommand cmd = (SqlCommand)command;
            cmd.Connection = (SqlConnection)SqlConnection;
            DataSet result = new DataSet();
            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) {
                adapter.Fill(result);
            }

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public DataTable ExecuteDataTable(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            SqlCommand cmd = (SqlCommand)command;
            cmd.Connection = (SqlConnection)SqlConnection;
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable result = new DataTable();
            adapter.Fill(result);
            adapter.Dispose();

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public int ExecuteNonQuery(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            SqlCommand cmd = (SqlCommand)command;
            cmd.Connection = (SqlConnection)SqlConnection;
            int result = cmd.ExecuteNonQuery();

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public bool ExecuteScalarBoolean(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            command.Connection = SqlConnection;
            command.CommandTimeout = CommandTimeout;
            bool result = DatabaseConverter.ToBoolean(command.ExecuteScalar());

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public decimal ExecuteScalarDecimal(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            command.Connection = SqlConnection;
            command.CommandTimeout = CommandTimeout;
            decimal result = DatabaseConverter.ToDecimal(command.ExecuteScalar());

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public Guid ExecuteScalarGuid(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            command.Connection = SqlConnection;
            command.CommandTimeout = CommandTimeout;

            Guid result = DatabaseConverter.ToGuid(command.ExecuteScalar());

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public int ExecuteScalarInteger(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            command.Connection = SqlConnection;
            command.CommandTimeout = CommandTimeout;
            int result = DatabaseConverter.ToInteger(command.ExecuteScalar());

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public long ExecuteScalarLong(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            command.Connection = SqlConnection;
            command.CommandTimeout = CommandTimeout;
            long result = DatabaseConverter.ToLong(command.ExecuteScalar());

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public string ExecuteScalarString(IDbCommand command) {
            if (AutoOpenClose) {
                Open();
            }

            command.Connection = SqlConnection;
            command.CommandTimeout = CommandTimeout;
            string result = DatabaseConverter.ToString(command.ExecuteScalar());

            if (AutoOpenClose) {
                Close();
            }

            return result;
        }
        public bool ExecuteDataTableBoolean(IDbCommand command) {
            DataTable dataTable = ExecuteDataTable(command);
            return !HasRows(dataTable) ? false : DatabaseConverter.ToBoolean(dataTable.Rows[0][0]);
        }
        public decimal ExecuteDataTableDecimal(IDbCommand command) {
            DataTable dataTable = ExecuteDataTable(command);
            return !HasRows(dataTable) ? 0 : DatabaseConverter.ToDecimal(dataTable.Rows[0][0]);
        }
        public DateTime ExecuteDataTableDateTime(IDbCommand command) {
            DataTable dataTable = ExecuteDataTable(command);
            return !HasRows(dataTable) ? DateTime.MinValue : DatabaseConverter.ToDateTime(dataTable.Rows[0][0]);
        }
        public Guid ExecuteDataTableGuid(IDbCommand command) {
            DataTable dataTable = ExecuteDataTable(command);
            return !HasRows(dataTable) ? Guid.Empty : DatabaseConverter.ToGuid(dataTable.Rows[0][0]);
        }
        public int ExecuteDataTableInteger(IDbCommand command) {
            DataTable dataTable = ExecuteDataTable(command);
            return !HasRows(dataTable) ? 0 : DatabaseConverter.ToInteger(dataTable.Rows[0][0]);
        }
        public long ExecuteDataTableLong(IDbCommand command) {
            DataTable dataTable = ExecuteDataTable(command);
            return !HasRows(dataTable) ? 0 : DatabaseConverter.ToLong(dataTable.Rows[0][0]);
        }
        public string ExecuteDataTableString(IDbCommand command) {
            DataTable dataTable = ExecuteDataTable(command);
            return !HasRows(dataTable) ? string.Empty : DatabaseConverter.ToString(dataTable.Rows[0][0]);
        }
        public T ExecuteDataTableType<T>(IDbCommand command) {
            DataTable dataTable = ExecuteDataTable(command);
            return !HasRows(dataTable) ? default(T) : (T)(dataTable.Rows[0][0]);
        }
        #endregion

        private bool HasRows(DataTable dataTable) {
            return dataTable != null && dataTable.Rows.Count >= 1;
        }
    }
}

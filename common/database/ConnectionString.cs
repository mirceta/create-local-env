namespace si.birokrat.next.common.database {
    public static class ConnectionString {
        public static string Format(
            string server,
            string database = "",
            string username = "",
            string password = "") {

            bool trustedConnection = string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password);
            var connectionString = $"Server={server};Trusted_Connection={trustedConnection};MultipleActiveResultSets=true;";
            if (!trustedConnection) {
                connectionString += $"User ID={username};Password={password};";
            }
            if (!string.IsNullOrEmpty(database)) {
                connectionString += $"Database={database}";
            }

            return connectionString;
        }
    }
}

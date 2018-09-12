using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveCameraSample
{
    public class AtosEmployeesDetails
    {
        public static AtosEmployee Get(string personId)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                // Define a t-SQL query string that has a parameter for orderID.
                const string sql = "SELECT * FROM dbo.AtosEmployees WHERE UserId = @personId";

                // Create a SqlCommand object.
                using (SqlCommand sqlCommand = new SqlCommand(sql, connection))
                {
                    // Define the @orderID parameter and set its value.
                    sqlCommand.Parameters.Add(new SqlParameter("@personId", SqlDbType.NVarChar));
                    sqlCommand.Parameters["@personId"].Value = personId;

                    try
                    {
                        connection.Open();

                        // Run the query by calling ExecuteReader().
                        using (SqlDataReader dataReader = sqlCommand.ExecuteReader())
                        {
                            // Create a data table to hold the retrieved data.
                            DataTable dataTable = new DataTable();

                            // Load the data from SqlDataReader into the data table.
                            dataTable.Load(dataReader);

                            // Display the data from the data table in the data grid view.
                            if (dataTable.Rows.Count > 0)
                            {
                                var first = dataTable.Rows[0];
                                AtosEmployee ae = new AtosEmployee()
                                {
                                    PersonId = first["UserId"].ToString(),
                                    FirstName = first["FirstName"].ToString(),
                                    LastName = first["LastName"].ToString(),
                                    DasId = first["DasId"].ToString(),
                                    Email = first["Email"].ToString()

                                };
                                dataReader.Close();
                                connection.Close();
                                return ae;

                            }
                            dataReader.Close();
                            connection.Close();
                            return null;
                            // Close the SqlDataReader.

                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        // Close the connection.
                        connection.Close();
                    }
                }
            }
            return null;
        }
    }
}

using Microsoft.Data.SqlClient;
using PointOfSaleApp.Models;
using System.Windows;

namespace PointOfSaleApp.Repositories
{
    public class ProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository()
        {
            // _connectionString = "Server=localhost;Database=PointOfSaleDB;Trusted_Connection=True;TrustServerCertificate=True;";
            _connectionString = "Server=localhost;Database=PointOfSaleDB;User Id = MerdyIlu; Password = Spearman84@; Persist Security Info = False; TrustServerCertificate = True;";
            //connectionString = "Server=localhost;Database=NbaDraftDB;User Id=MerdyIlu;Password=Spearman84@;Persist Security Info=False;TrustServerCertificate=True;"
            //_connectionString = "Server=localhost;Database=PointOfSaleDB;User Id =jc; Password =Patate123; Persist Security Info = False; TrustServerCertificate = True;";
        }

        public void AddProduct(Product product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "INSERT INTO Products (Barcode, Name, Price, Description) VALUES (@Barcode, @Name, @Price, @Description)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Barcode", product.Barcode);
                    command.Parameters.AddWithValue("@Name", product.Name);
                    command.Parameters.AddWithValue("@Price", product.Price);
                    command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Product GetProductByBarcode(string barcode)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Barcode, Name, Price, Description FROM Products WHERE Barcode = @Barcode";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Barcode", barcode);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Product
                            {
                                Id = reader.GetInt32(0),
                                Barcode = reader.GetString(1),
                                Name = reader.GetString(2),
                                Price = reader.GetDecimal(3),
                                Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public List<RFIDScan> GetAllRFIDScans()
        {
            try
            {
                List<RFIDScan> scanHistory = new List<RFIDScan>();
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT ScanID, CardID, ScanTime, Role, Access
                        FROM RFIDScans ORDER BY ScanTime DESC ";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                scanHistory.Add(new RFIDScan
                                {
                                    ScanID = reader.GetInt32(0),
                                    CardID = reader.GetString(1),
                                    ScanTime = reader.GetDateTime(2),
                                    Role = reader.IsDBNull(3) ? "Non défini" : reader.GetString(3),
                                    Access = reader.GetString(4)
                                });
                            }
                        }
                    }
                }
                return scanHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement de l'historique des scans : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<RFIDScan>();
            }

        }

        public List<RFIDUser> GetAllRFIDUsers()
        {
            try
            {
                List<RFIDUser> users = new List<RFIDUser>();
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT IdUser, CardID, Nom
                        FROM RFIDUser";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new RFIDUser
                                {
                                    IdUser = reader.GetInt32(0),
                                    CardID = reader.GetString(1),
                                    Name = reader.GetString(2)
                                });
                            }
                        }
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des utilisateurs : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<RFIDUser>();
            }
        }

        public void AddUser(RFIDUser user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "INSERT INTO RFIDUser (CardID, Nom) VALUES (@CardID, @Nom)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CardID", user.CardID);
                    command.Parameters.AddWithValue("@Nom", user.Name);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteUser(string userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "DELETE FROM RFIDUser WHERE IdUser = @IdUser";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUser", userId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
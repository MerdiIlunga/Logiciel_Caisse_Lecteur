using Microsoft.Data.SqlClient;
using PointOfSaleApp.Models;

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

        public List<Product> GetAllProducts()
        {
            var products = new List<Product>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Barcode, Name, Price, Description FROM Products";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                Id = reader.GetInt32(0),
                                Barcode = reader.GetString(1),
                                Name = reader.GetString(2),
                                Price = reader.GetDecimal(3),
                                Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return products;
        }
    }
}
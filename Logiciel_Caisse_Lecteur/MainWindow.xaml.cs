using PointOfSaleApp.Models;
using PointOfSaleApp.Repositories;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using ZXing;
using ZXing.Common;

namespace PointOfSaleApp
{
    public partial class MainWindow : Window
    {
        private readonly ProductRepository _productRepository;
        private readonly List<Product> _scannedProducts = new List<Product>();
        private string _currentBarcode = "";
        private string _currentInput = "";
        private DispatcherTimer _scanTimer;
        private List<RFIDUser> users = new List<RFIDUser>();
        private List<RFIDScan> scanHistory = new List<RFIDScan>();
        private Bitmap _barcodeBitmap;

        public MainWindow()
        {
            InitializeComponent();
            _productRepository = new ProductRepository();
            BarcodeInput.Focus();

            _scanTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _scanTimer.Tick += ScanTimer_Tick;

            LoadUsers();
            LoadScanHistory();
        }

        private void GenerateBarcodeButton_Click(object sender, RoutedEventArgs e)
        {
            string input = BarcodeGenerateInput.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                StatusText.Text = "Veuillez entrer un numéro valide pour le code-barres.";
                return;
            }

            try
            {
                var barcodeWriter = new BarcodeWriter<Bitmap>
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Width = 200,
                        Height = 100,
                        Margin = 10,
                        PureBarcode = false
                    }
                };

                _barcodeBitmap = barcodeWriter.Write(input);
                PrintBarcode();
                StatusText.Text = $"Code-barres {input} généré et envoyé à l'impression.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erreur lors de la génération du code-barres : {ex.Message}";
            }
        }

        private void PrintBarcode()
        {
            if (_barcodeBitmap == null)
            {
                StatusText.Text = "Aucun code-barres à imprimer.";
                return;
            }

            try
            {
                PrintDocument printDocument = new PrintDocument();
                printDocument.PrintPage += (s, args) =>
                {
                    float x = (args.PageBounds.Width - _barcodeBitmap.Width) / 2;
                    float y = 50;
                    args.Graphics.DrawImage(_barcodeBitmap, x, y);
                };

                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDocument.Print();
                    StatusText.Text = "Code-barres imprimé avec succès.";
                }
                else
                {
                    StatusText.Text = "Impression du code-barres annulée.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erreur lors de l'impression : {ex.Message}";
            }
            finally
            {
                _barcodeBitmap?.Dispose();
                _barcodeBitmap = null;
            }
        }

        private void BarcodeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentInput = BarcodeInput.Text;
            StatusText.Text = $"Entrée reçue : '{_currentInput}'";
            _scanTimer.Stop();
            _scanTimer.Start();
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            _scanTimer.Stop();
            if (!string.IsNullOrEmpty(_currentInput))
            {
                _currentBarcode = _currentInput.Trim();
                StatusText.Text = $"Code-barres scanné : {_currentBarcode}";
                LastScannedBarcode.Text = _currentBarcode;
                _currentInput = "";
                BarcodeInput.Text = "";
            }
            else
            {
                StatusText.Text = "Aucune entrée détectée.";
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentBarcode))
            {
                StatusText.Text = "Veuillez scanner un code-barres d'abord.";
                return;
            }

            if (string.IsNullOrEmpty(ProductNameInput.Text) || string.IsNullOrEmpty(PriceInput.Text))
            {
                StatusText.Text = "Veuillez remplir le nom et le prix du produit.";
                return;
            }

            if (!decimal.TryParse(PriceInput.Text, out decimal price))
            {
                StatusText.Text = "Prix invalide.";
                return;
            }

            var product = new Product
            {
                Barcode = _currentBarcode,
                Name = ProductNameInput.Text,
                Price = price,
                Description = DescriptionInput.Text
            };

            try
            {
                _productRepository.AddProduct(product);
                StatusText.Text = $"Produit '{product.Name}' associé au code-barres {_currentBarcode}.";
                ClearProductForm();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erreur : {ex.Message}";
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentBarcode))
            {
                StatusText.Text = "Veuillez scanner un code-barres d'abord.";
                return;
            }

            try
            {
                var product = _productRepository.GetProductByBarcode(_currentBarcode);
                if (product != null)
                {
                    _scannedProducts.Add(product);
                    ScannedProductsList.ItemsSource = null;
                    ScannedProductsList.ItemsSource = _scannedProducts;
                    TotalText.Text = $"Total : {CalculateTotal():C}";
                    StatusText.Text = $"Produit trouvé : {product.Name} ({product.Price:C})";
                }
                else
                {
                    StatusText.Text = $"Produit non trouvé pour le code-barres {_currentBarcode}.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erreur : {ex.Message}";
            }
        }

        private void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_scannedProducts.Count == 0)
            {
                StatusText.Text = "Aucun produit à imprimer.";
                return;
            }

            try
            {
                StatusText.Text = "Ouverture de la boîte de dialogue d'impression...";
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    FlowDocument document = new FlowDocument
                    {
                        PageWidth = printDialog.PrintableAreaWidth,
                        PageHeight = printDialog.PrintableAreaHeight,
                        ColumnWidth = double.NaN,
                        FontFamily = new System.Windows.Media.FontFamily("Times New Roman"),
                        FontSize = 12
                    };

                    Paragraph header = new Paragraph(new Run("Reçu Officiel"))
                    {
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.Navy,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(header);

                    Paragraph subHeader = new Paragraph(new Run("Skibiddy - Commerce #12345"))
                    {
                        FontSize = 14,
                        FontStyle = FontStyles.Italic,
                        Foreground = System.Windows.Media.Brushes.DarkBlue,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(subHeader);

                    Paragraph address = new Paragraph(new Run("325 rue Saint-Pierre, Rivière-du-loup.QC"))
                    {
                        FontSize = 12,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(address);

                    Paragraph date = new Paragraph(new Run($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}"))
                    {
                        FontSize = 12,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(date);

                    document.Blocks.Add(new Paragraph(new Run("")) { Margin = new Thickness(0, 10, 0, 0) });

                    Paragraph separator = new Paragraph(new Run("--------------------------------"))
                    {
                        FontSize = 12,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        TextAlignment = TextAlignment.Left
                    };
                    document.Blocks.Add(separator);

                    Paragraph columnHeaders = new Paragraph(new Run("Code-Barres         Nom                Prix"))
                    {
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.Navy,
                        TextAlignment = TextAlignment.Left,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    document.Blocks.Add(columnHeaders);

                    foreach (var product in _scannedProducts)
                    {
                        Paragraph productLine = new Paragraph(new Run(
                            $"{product.Barcode,-16} {product.Name,-20} {product.Price,10:C}"))
                        {
                            FontSize = 12,
                            TextAlignment = TextAlignment.Left,
                            Margin = new Thickness(0, 0, 0, 2)
                        };
                        document.Blocks.Add(productLine);
                    }

                    document.Blocks.Add(new Paragraph(new Run("")) { Margin = new Thickness(0, 10, 0, 0) });

                    document.Blocks.Add(separator);

                    decimal subTotal = CalculateSubTotal();
                    decimal tps = subTotal * 0.05m;
                    decimal tvq = subTotal * 0.09975m;
                    decimal total = subTotal + tps + tvq;

                    Paragraph subTotalLine = new Paragraph(new Run($"Sous-Total: {subTotal:C}"))
                    {
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 0, 20, 2)
                    };
                    document.Blocks.Add(subTotalLine);

                    Paragraph tpsLine = new Paragraph(new Run($"TPS (5%): {tps:C}"))
                    {
                        FontSize = 12,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 0, 20, 2)
                    };
                    document.Blocks.Add(tpsLine);

                    Paragraph tvqLine = new Paragraph(new Run($"TVQ (9,975%): {tvq:C}"))
                    {
                        FontSize = 12,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 0, 20, 2)
                    };
                    document.Blocks.Add(tvqLine);

                    Paragraph totalLine = new Paragraph(new Run($"Total: {total:C}"))
                    {
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.DarkRed,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 10, 20, 0)
                    };
                    document.Blocks.Add(totalLine);

                    document.Blocks.Add(new Paragraph(new Run("")) { Margin = new Thickness(0, 15, 0, 0) });

                    Paragraph thankYou = new Paragraph(new Run("Au plaisir de vous revoir !"))
                    {
                        FontSize = 12,
                        FontStyle = FontStyles.Italic,
                        Foreground = System.Windows.Media.Brushes.Green,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(thankYou);

                    StatusText.Text = "Impression en cours...";
                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Reçu Officiel Point de Vente");

                    StatusText.Text = "Reçu imprimé avec succès.";
                }
                else
                {
                    StatusText.Text = "Impression annulée.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erreur d'impression : {ex.Message}";
            }
        }

        private void ClearBarcodeButton_Click(object sender, RoutedEventArgs e)
        {
            _currentBarcode = "";
            LastScannedBarcode.Text = "Aucun";
            StatusText.Text = "Code-barres effacé. En attente de scan...";
        }

        private decimal CalculateSubTotal()
        {
            decimal subTotal = 0;
            foreach (var product in _scannedProducts)
            {
                subTotal += product.Price;
            }
            return subTotal;
        }

        private decimal CalculateTotal()
        {
            return CalculateSubTotal();
        }

        private void ClearProductForm()
        {
            ProductNameInput.Text = "";
            PriceInput.Text = "";
            DescriptionInput.Text = "";
        }

        private void BarcodeInput_GotFocus(object sender, RoutedEventArgs e)
        {
            BarcodeInput.Focus();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            BarcodeInput.Focus();
        }

        private void LoadUsers()
        {
            users.Clear();
            users = _productRepository.GetAllRFIDUsers();
            UsersList.ItemsSource = null;
            UsersList.ItemsSource = users;
        }

        private void LoadScanHistory()
        {
            scanHistory.Clear();
            scanHistory = _productRepository.GetAllRFIDScans();
            ScanHistoryList.ItemsSource = null;
            ScanHistoryList.ItemsSource = scanHistory;
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(UserIdInput.Text) || string.IsNullOrEmpty(UserNameInput.Text))
                {
                    MessageBox.Show("Veuillez saisir l'ID de l'utilisateur et son nom.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string cardId = UserIdInput.Text.Trim();
                string name = UserNameInput.Text.Trim();

                RFIDUser newUser = new RFIDUser
                {
                    CardID = cardId,
                    Name = name,
                };

                _productRepository.AddUser(newUser);
                MessageBox.Show("Utilisateur ajouté avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'ajout de l'utilisateur : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = (Button)sender;
                string userId = button.Tag.ToString();

                MessageBoxResult result = MessageBox.Show(
                    "Êtes-vous sûr de vouloir supprimer l'utilisateur " + userId + "?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _productRepository.DeleteUser(userId);
                    LoadUsers();
                    MessageBox.Show("Utilisateur supprimé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la suppression de l'utilisateur : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            LoadScanHistory();
            LoadUsers();
        }
    }
}
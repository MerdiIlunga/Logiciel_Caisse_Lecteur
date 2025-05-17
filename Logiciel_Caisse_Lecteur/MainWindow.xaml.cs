using PointOfSaleApp.Models;
using PointOfSaleApp.Repositories;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace PointOfSaleApp
{
    public partial class MainWindow : Window
    {
        private readonly ProductRepository _productRepository;
        private readonly List<Product> _scannedProducts = new List<Product>();
        private string _currentBarcode = "";
        private string _currentInput = "";
        private DispatcherTimer _scanTimer; // Timer pour détecter la fin du scan

        public MainWindow()
        {
            InitializeComponent();
            _productRepository = new ProductRepository();
            BarcodeInput.Focus();

            // Initialiser le timer
            _scanTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Délai de 100 ms
            };
            _scanTimer.Tick += ScanTimer_Tick;
        }

        private void BarcodeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentInput = BarcodeInput.Text;

            // Débogage détaillé
            StatusText.Text = $"Entrée reçue : '{_currentInput}'";

            // Redémarrer le timer à chaque changement de texte
            _scanTimer.Stop();
            _scanTimer.Start();
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            // Le timer a expiré, donc on considère que le scan est terminé
            _scanTimer.Stop();

            if (!string.IsNullOrEmpty(_currentInput))
            {
                _currentBarcode = _currentInput.Trim();
                StatusText.Text = $"Code-barres scanné : {_currentBarcode}";
                LastScannedBarcode.Text = _currentBarcode; // Mettre à jour LastScannedBarcode
                _currentInput = "";
                BarcodeInput.Text = ""; // Effacer après scan
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
                // Créer un PrintDialog pour choisir une imprimante
                StatusText.Text = "Ouverture de la boîte de dialogue d'impression...";
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Créer un FlowDocument pour formater le reçu
                    FlowDocument document = new FlowDocument
                    {
                        PageWidth = printDialog.PrintableAreaWidth,
                        PageHeight = printDialog.PrintableAreaHeight,
                        ColumnWidth = double.NaN, // Ajuster à la largeur disponible
                        FontFamily = new FontFamily("Times New Roman"),
                        FontSize = 12
                    };

                    // En-tête stylisé
                    Paragraph header = new Paragraph(new Run("Reçu Officiel"))
                    {
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Navy,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(header);

                    Paragraph subHeader = new Paragraph(new Run("Skibiddy - Commerce #12345"))
                    {
                        FontSize = 14,
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.DarkBlue,
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

                    document.Blocks.Add(new Paragraph(new Run("")) { Margin = new Thickness(0, 10, 0, 0) }); // Espace

                    // Ligne de séparation
                    Paragraph separator = new Paragraph(new Run("--------------------------------"))
                    {
                        FontSize = 12,
                        Foreground = Brushes.Gray,
                        TextAlignment = TextAlignment.Left
                    };
                    document.Blocks.Add(separator);

                    // En-têtes de colonnes
                    Paragraph columnHeaders = new Paragraph(new Run("Code-Barres         Nom                Prix"))
                    {
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Navy,
                        TextAlignment = TextAlignment.Left,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    document.Blocks.Add(columnHeaders);

                    // Liste des produits
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

                    document.Blocks.Add(new Paragraph(new Run("")) { Margin = new Thickness(0, 10, 0, 0) }); // Espace

                    // Ligne de séparation
                    document.Blocks.Add(separator);

                    // Calcul des taxes canadiennes
                    decimal subTotal = CalculateSubTotal();
                    decimal tps = subTotal * 0.05m; // TPS 5%
                    decimal tvq = subTotal * 0.09975m; // TVQ 9,975% (Québec)
                    decimal total = subTotal + tps + tvq;

                    // Sous-total
                    Paragraph subTotalLine = new Paragraph(new Run($"Sous-Total: {subTotal:C}"))
                    {
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 0, 20, 2)
                    };
                    document.Blocks.Add(subTotalLine);

                    // TPS
                    Paragraph tpsLine = new Paragraph(new Run($"TPS (5%): {tps:C}"))
                    {
                        FontSize = 12,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 0, 20, 2)
                    };
                    document.Blocks.Add(tpsLine);

                    // TVQ
                    Paragraph tvqLine = new Paragraph(new Run($"TVQ (9,975%): {tvq:C}"))
                    {
                        FontSize = 12,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 0, 20, 2)
                    };
                    document.Blocks.Add(tvqLine);

                    // Total
                    Paragraph totalLine = new Paragraph(new Run($"Total: {total:C}"))
                    {
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.DarkRed,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 10, 20, 0)
                    };
                    document.Blocks.Add(totalLine);

                    document.Blocks.Add(new Paragraph(new Run("")) { Margin = new Thickness(0, 15, 0, 0) }); // Espace

                    // Message de fin
                    Paragraph thankYou = new Paragraph(new Run("Au plaisir de vous revoir !"))
                    {
                        FontSize = 12,
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.Green,
                        TextAlignment = TextAlignment.Center
                    };
                    document.Blocks.Add(thankYou);

                    // Imprimer le document
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
            return CalculateSubTotal(); // Pour l'affichage dans l'UI, avant taxes
        }

        private void ClearProductForm()
        {
            ProductNameInput.Text = "";
            PriceInput.Text = "";
            DescriptionInput.Text = "";
            // Ne pas effacer _currentBarcode ici pour le conserver
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
    }
}
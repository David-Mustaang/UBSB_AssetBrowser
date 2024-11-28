using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace UBSB_AssetBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string apiLink = "https://dl.u7-trainz.de/api/assets.json";
        string? fromRevision;
        string? defaultSearchString;
        string? defaultStatusLabelContent;
        Brush defaultStatusLabelColor;
        // string apiLinkFromRevision = "$https://dl.u7-trainz.de/api/assets.json?revision={fromRevision}";  // TODO / Vorbereitung: Zeige veränderte Assets nach Revision X
        string? downloadFileID;
        string? downloadRevision;
        string? downloadName;
        string downloadLink;
        bool firstInitialized = false;
        bool inputSucheIsEmpty = true;

        string jsonData;
        Root? root;
        List<Assets> filteredAssets = new();


        public async void ReadJSON()
        {
            try
            {
                // GUI deaktivieren
                input_Suche.IsEnabled = false;
                button_clearSearch.IsEnabled = false;
                button_link.IsEnabled = false;
                button_download.IsEnabled = false;

                label_status.Content = "Einen Moment bitte...";
                label_status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dd0000"));

                // Hole Daten von der API
                jsonData = await FetchAssetsFromAPI();

                label_status.Content = "Assetliste wurde erfolgreich geladen!";
                label_status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#009900"));

                // JSON deserialisieren
                root = JsonSerializer.Deserialize<Root>(jsonData);

                // Setze ItemsSource für DataGrid
                datagrid_assets.ItemsSource = root.assets;

                // GUI aktivieren
                input_Suche.IsEnabled = true;
                button_clearSearch.IsEnabled = true;
                button_link.IsEnabled = true;
                button_download.IsEnabled = true;

                // Setze Default-Werte für später
                defaultStatusLabelContent = label_status.Content.ToString();
                defaultStatusLabelColor = label_status.Foreground;
            }
            catch (Exception)
            {
                MessageBox.Show("Konnte auf Daten von API nicht zugreifen!\nBitte überprüfe deine Internetverbindung und versuche es erneut!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);

                label_status.Content = "Assetliste konnte nicht geladen werden";
                label_status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dd0000"));
            }
        }

        public async Task<String> FetchAssetsFromAPI()
        {
            HttpClient httpClient = new();

            // Hole die JSON-Daten von der API
            string jsonResponse = await httpClient.GetStringAsync(apiLink);

            // Rückgabe der JSON-Daten
            return jsonResponse;
        }

        public void UpdateFilter()
        {
            var collectionView = CollectionViewSource.GetDefaultView(datagrid_assets.ItemsSource);

            // Wenn inputSucheIsEmpty true ist, zeige alle Assets an
            if (inputSucheIsEmpty)
            {
                collectionView.Filter = null; // Entfernt den Filter, zeigt alles an
            }
            else
            {
                collectionView.Filter = obj =>
                {
                    if (obj is not Assets asset) return false;

                    return asset.username.Contains(input_Suche.Text, StringComparison.OrdinalIgnoreCase) ||
                           asset.kuid.Contains(input_Suche.Text, StringComparison.OrdinalIgnoreCase);
                };
            }

            collectionView.Refresh();
        }

        public async Task WaitXMilliseconds(int timer)
        {
            await Task.Delay(timer);
        }

        public async Task DownloadAsset()
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = $"{downloadName}_r{downloadRevision}.zip",
                    Filter = "Alle Dateien (*.*)|*.*",
                    Title = "Asset speichern unter..."
                };

                // Zeigt saveFileDialog und führt aus, wenn Speichern gewählt wurde
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    label_status.Content = $"{downloadName} wird heruntergeladen...";

                    // Deaktiviere Download-Button, bis Download abgeschlossen ist.
                    button_download.IsEnabled = false;

                    // Lade Asset herunter
                    HttpClient httpClient = new();
                    HttpResponseMessage response = await httpClient.GetAsync(downloadLink);  // Downloadlink wurde vor Methodenaufruf gesetzt

                    // Wenn Download erfolgreich, dann:
                    if (response.IsSuccessStatusCode)
                    {
                        // Lese den Inhalt als Stream
                        Stream contentStream = await response.Content.ReadAsStreamAsync();

                        // Schreibe den Stream in die ausgewählte Datei
                        FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await contentStream.CopyToAsync(fileStream);
                        fileStream.Close();

                        // Erfolgsmeldung + Datei öffnen
                        label_status.Content = $"{downloadName} wurde erfolgreich heruntergeladen";
                        button_download.IsEnabled = true;
                        await WaitXMilliseconds(500);

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }

                    // Wenn Download nicht erfolgreich, dann:
                    else
                    {
                        MessageBox.Show($"Fehler beim Herunterladen der Datei: {response.StatusCode}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        
                        label_status.Content = "Der Download wurde aufgrund eines Fehlers abgebrochen!";
                        label_status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dd0000"));
                        button_download.IsEnabled = true;
                    }

                }

                // Wenn User im SaveFileDialog "Abbrechen" auswählt
                else
                {
                    label_status.Content = "Der Download wurde abgebrochen!";
                    label_status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dd0000"));
                }

            }

            // Wenn ein anderer Fehler auftritt
            catch (Exception ex)
            {
                MessageBox.Show($"Ein Fehler ist aufgetreten: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                label_status.Content = "Der Download wurde aufgrund eines Fehlers abgebrochen!";
                label_status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dd0000"));
                button_download.IsEnabled = true;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            ReadJSON();
            firstInitialized = true;

            defaultSearchString = input_Suche.Text;
            // defaultStatusLabelContent in ReadJSON gesetzt
            // defaultStatusColor in ReadJSON gesetzt
        }

        private void input_Suche_GotFocus(object sender, RoutedEventArgs e)
        {
            if (inputSucheIsEmpty == true)
            {
                inputSucheIsEmpty = false;
                input_Suche.Text = "";
                input_Suche.Foreground = Brushes.Black;
            }
        }

        private void input_Suche_LostFocus(object sender, RoutedEventArgs e)
        {
            if (input_Suche.Text.TrimEnd() == "")
            {
                inputSucheIsEmpty = true;
                input_Suche.Text = defaultSearchString;
                input_Suche.Foreground = Brushes.Gray;
            }
        }

        private void input_Suche_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (firstInitialized)
            {
                UpdateFilter();
            }
        }

        private void button_clearSearch_Click(object sender, RoutedEventArgs e)
        {
            input_Suche.Text = "";
            input_Suche_LostFocus(sender, e);
            datagrid_assets.SelectedItem = null;
        }

        private void datagrid_assets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedAsset = datagrid_assets.SelectedItem as Assets;

            if (selectedAsset != null)
            {
                downloadFileID = selectedAsset.fileId;
                downloadRevision = selectedAsset.revision.ToString();
                downloadName = selectedAsset.username;
            }

            else
            {
                downloadFileID = null;
                downloadRevision = null;
                downloadName = null;
            }
        }

            private void button_exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void button_link_Click(object sender, RoutedEventArgs e)
        {
            // Ausführen, wenn eine Datei ausgewählt wurde
            if (downloadFileID != null && downloadRevision != null)
            {
                // Erstelle Downloadlink, kopiere in Zwischenablage und gebe Erfolgsmeldung
                downloadLink = $"https://dl.u7-trainz.de/assets-new/low/{downloadFileID}_r{downloadRevision}.zip";
                Clipboard.SetText(downloadLink);
                label_status.Content = "Downloadlink wurde in die Zwischenablage kopiert";
                label_status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#009900"));

                // Nach 5 Sekunden zu altem Status-Label zurückwechseln
                await WaitXMilliseconds(5000);

                label_status.Content = defaultStatusLabelContent;
                label_status.Foreground = defaultStatusLabelColor;
            }

            else
            {
                MessageBox.Show("Bitte wähle zuerst ein Asset in der Liste aus!", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void button_download_Click(object sender, RoutedEventArgs e)
        {
            // Ausführen, wenn eine Datei ausgewählt wurde
            if (downloadFileID != null && downloadRevision != null && downloadName != null)
            {
                // Erstelle Download-Link und 
                downloadLink = $"https://dl.u7-trainz.de/assets-new/low/{downloadFileID}_r{downloadRevision}.zip";

                label_status.Content = "Download wird gestartet...";
                label_status.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#009900"));

                // Initialisiere Download
                await DownloadAsset();

                // 5 Sekunden nach Abschluss des Downloads zu altem Label zurückwechseln
                await WaitXMilliseconds(5000);

                label_status.Content = defaultStatusLabelContent;
                label_status.Foreground = defaultStatusLabelColor;
            }

            else
            {
                MessageBox.Show("Bitte wähle zuerst ein Asset in der Liste aus!", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Xml;

namespace BackendUkol_WPF_JKupka
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// Vytvořil: Jaromír Kupka
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfd = new OpenFileDialog
            {
                InitialDirectory = Environment.CurrentDirectory,
                Title = "Browse XML Files",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "xml",
                Filter = "XML files (*.xml)|*.xml",
            };

            bool? dialogOK = openfd.ShowDialog();

            if (dialogOK == true)
            {
                tbxFileRoute.Text = openfd.FileName;
            }
        }

        private void BtnUse_Click(object sender, RoutedEventArgs e)
        {
            string filePath = tbxFileRoute.Text;
            DataTable myTable;

            //ošetření
            if (filePath == null || filePath == "")
            {
                MessageBox.Show("Zadejte cestu k souboru.");
                return;
            }
            else
            { myTable = LoadTable(filePath); }

            //zobrazí vložené data
            ShowInputTable(myTable);
            
            //zpracování vložených dat
            DataTable resultTable = ProcessInputData(myTable);

            //zobrazý zpracovaná data
            ShowResultTable(resultTable);
        }

        private DataTable CreateEmptyInputTable()
        {
            DataTable myTable = new DataTable("CarSalesTable");
            myTable.Columns.Add("ModelName", typeof(string));
            myTable.Columns.Add("SaleDate", typeof(DateTime));
            myTable.Columns.Add("Price", typeof(double));
            myTable.Columns.Add("DPH", typeof(double));
            return myTable;
        }

        private DataTable LoadTable(string filePath)
        {
            DataTable myTable = CreateEmptyInputTable();
            
            //proměné pro dočasné držení dat
            string tempModelName;
            string tempStringDate;
            string tempStringPrice;
            string tempStringDPH;
            DateTime tempSaleDate;
            double tempPrice;
            double tempDPH;

            try
            {
                //Načtení XML souboru
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                //Naplnění tabulky z XML souboru
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    //předat data
                    tempModelName = node["NazevModelu"].InnerText;
                    tempStringDate = node["DatumProdeje"].InnerText;
                    tempStringPrice = node["Cena"].InnerText;
                    tempStringDPH = node["DPH"].InnerText;

                    //převést string na double
                    tempPrice = Convert.ToDouble(tempStringPrice);
                    tempDPH = Convert.ToDouble(tempStringDPH);

                    //převést string do DateTime
                    tempSaleDate = DateTime.Parse(tempStringDate);

                    //přidat řádek do tabulky
                    myTable.Rows.Add(tempModelName, tempSaleDate, tempPrice, tempDPH);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Can't parse xml file.", filePath), ex);
            }

            return myTable;
        }

        private DataTable ProcessInputData(DataTable table)
        {
            //vybere pouze výkendy
            DataTable weekendOnlyTable = PickWeekendOnly(table);

            //vybere unikátní názvy modelů
            List<string> models = PickDistinctModels(table);

            //vypočítá tržby pro každý model
            DataTable resultTable = CalculateSalesPerModel(weekendOnlyTable, models);

            return resultTable;
        }

        private DataTable PickWeekendOnly(DataTable table)
        {
            DataTable weekendOnlyTable = CreateEmptyInputTable();

            foreach (DataRow drow in table.Rows)
            {
                DateTime den = (DateTime)drow[1];
                DayOfWeek day = den.DayOfWeek;
                if ((day == DayOfWeek.Saturday) || (day == DayOfWeek.Sunday))
                {
                    weekendOnlyTable.Rows.Add(drow.ItemArray);
                }
            }

            return weekendOnlyTable;
        }

        private List<string> PickDistinctModels(DataTable table)
        {
            DataTable distinctTable = table.DefaultView.ToTable(true, "ModelName");
            List<string> models = new List<string>();

            foreach (DataRow drow in distinctTable.Rows)
            {
                models.Add((string)drow[0]);
            }

            return models;
        }

        private DataTable CalculateSalesPerModel(DataTable table, List<string> models)
        {
            //vytvořit tabulku pro výsledek
            DataTable resultTable = new DataTable("WeekendCarSalesTable");
            resultTable.Columns.Add("Název Modelu", typeof(string));
            resultTable.Columns.Add("Cena bez DPH", typeof(double));
            resultTable.Columns.Add("Cena s DPH", typeof(double));

            //proměné pro kalkulaci
            double sto = 100;
            double noDPH = 0;
            double withDPH = 0;
            double koefDPH;
            double calc;

            //sečte prodeje pro každý model
            foreach (string m in models)
            {
                foreach (DataRow drow in table.Rows)
                {
                    if ((string)drow[0] == m)
                    {
                        noDPH += (double)drow[2];
                        koefDPH = sto + (double)drow[3];
                        koefDPH = koefDPH / sto;
                        calc = (double)drow[2] * koefDPH;
                        withDPH += calc;
                    }
                }

                resultTable.Rows.Add(m, noDPH, withDPH);

                //vynulovat hodnoty
                noDPH = 0;
                withDPH = 0;
            }

            return resultTable;
        }

        private void ShowResultTable(DataTable resultTable)
        {
            dtgOutputTable.DataContext = resultTable;
        }

        private void ShowInputTable(DataTable showTable)
        {
            dtgInputTable.DataContext = showTable;
        }
    }
}
// Vytvořil: Jaromír Kupka
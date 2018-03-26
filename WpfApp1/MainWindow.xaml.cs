using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace WpfApp1 {
    public class UsuarioModel {
        public string Usuario {
            get; set;
        }
        public string Password {
            get; set;
        }
    }
    public class ConnStringModel {
        public string DataSource {
            get; set;
        }
        public string Catalog {
            get; set;
        }
        public string User {
            get; set;
        }
        public string Passwd {
            get; set;
        }
        public bool IntSecurity {
            get; set;
        }
        public override string ToString() {
            if (!IntSecurity)
                return string.Format("Data Source={0}; Initial Catalog={1}; User id={2}; Password={3};", DataSource, Catalog, User, Passwd);
            else
                return string.Format("Data Source={0}; Initial Catalog={1}; Integrated Security=true;", DataSource, Catalog);
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        const string K_SETTINGS = "settings.xml";
        const string K_SRV_SETTINGS = "servers.xml";

        private SqlCommand Cmd;
        private SqlConnection Conn;
        private SqlDataReader dr;

        public string ConnStringOrigen {
            get; private set;
        }
        public string ConnStringDestino {
            get; private set;
        }

        private ConnStringModel ConnStrModelOrigen {
            get; set;
        }
        private ConnStringModel ConnStrModelDestino {
            get; set;
        }
        private UsuarioModel Usuario;

        public MainWindow() {
            InitializeComponent();
            Usuario = new UsuarioModel();
            ConnStrModelOrigen = new ConnStringModel();
            ConnStrModelDestino = new ConnStringModel();
            if (LoadSettings()) {
                FrmLogin login = new FrmLogin() {
                    Usuario = Usuario
                };
                bool? resultado = login.ShowDialog();
                if (resultado.HasValue ? !resultado.Value : true) {
                    Close();
                }
            }
            #region asignación de valores a controles
            txtUsuario.Text = Usuario.Usuario;

            txtServOrigen.Text = ConnStrModelOrigen.DataSource;
            txtBDOrigen.Text = ConnStrModelOrigen.Catalog;
            txtUsrOrigen.Text = ConnStrModelOrigen.User;
            chkSeguridadOrigen.IsChecked = ConnStrModelOrigen.IntSecurity;

            txtServDestino.Text = ConnStrModelDestino.DataSource;
            txtBDDestino.Text = ConnStrModelDestino.Catalog;
            txtUsrDestino.Text = ConnStrModelDestino.User;
            chkSeguridadDestino.IsChecked = ConnStrModelDestino.IntSecurity;

            txbConnStrOrigen.Text = ConnStrModelOrigen.ToString();
            txbConnStrDestino.Text = ConnStrModelDestino.ToString();
            #endregion
        }

        private bool LoadSettings() {
            try {
                #region User settings
                XmlReader reader = XmlReader.Create(K_SETTINGS);
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "User") {
                        Usuario.Usuario = reader.GetAttribute("ID").ToLower();
                        while (reader.NodeType != XmlNodeType.EndElement) {
                            reader.Read();
                            if (reader.Name == "PASS") {
                                while (reader.NodeType != XmlNodeType.EndElement) {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        Usuario.Password = reader.Value;
                                }
                                reader.Read();
                            }
                        }
                    }
                }
                #endregion
                #region servers settings
                reader = XmlReader.Create(K_SRV_SETTINGS);
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "Servidor") {
                        if (reader.GetAttribute("Tipo").Equals("Origen")) {
                            ConnStrModelOrigen.DataSource = reader.GetAttribute("DataSource");
                            ConnStrModelOrigen.Catalog = reader.GetAttribute("Catalog");
                            ConnStrModelOrigen.User = reader.GetAttribute("User");
                            ConnStrModelOrigen.IntSecurity = reader.GetAttribute("IntegratedSecurity") == "True";
                        }
                        if (reader.GetAttribute("Tipo").Equals("Destino")) {
                            ConnStrModelDestino.DataSource = reader.GetAttribute("DataSource");
                            ConnStrModelDestino.Catalog = reader.GetAttribute("Catalog");
                            ConnStrModelDestino.User = reader.GetAttribute("User");
                            ConnStrModelDestino.IntSecurity = reader.GetAttribute("IntegratedSecurity") == "True";
                        }
                    }
                }
                #endregion
                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        private void chkSeguridadOrigen_Checked(object sender, RoutedEventArgs e) {
            txtUsrOrigen.IsEnabled = (sender as CheckBox).IsChecked.HasValue ? (sender as CheckBox).IsChecked.Value : true;
            txtPassOrigen.IsEnabled = (sender as CheckBox).IsChecked.HasValue ? (sender as CheckBox).IsChecked.Value : true;
        }

        private void chkSeguridadDestino_Checked(object sender, RoutedEventArgs e) {
            txtUsrDestino.IsEnabled = (sender as CheckBox).IsChecked.HasValue ? (sender as CheckBox).IsChecked.Value : true;
            txtPassDestino.IsEnabled = (sender as CheckBox).IsChecked.HasValue ? (sender as CheckBox).IsChecked.Value : true;
        }

        private void btnGuardarUsuario_Click(object sender, RoutedEventArgs e) {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            Usuario.Usuario = txtUsuario.Text;
            Usuario.Password = txtPassword.Password;

            XmlWriter writer = XmlWriter.Create(K_SETTINGS, settings);
            writer.WriteStartDocument();
            writer.WriteComment("This file is generated by the program.");
            writer.WriteStartElement("User");
            writer.WriteAttributeString("ID", Usuario.Usuario);
            writer.WriteElementString("PASS", Usuario.Password);
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();

        }

        private void btnGuardarServidores_Click(object sender, RoutedEventArgs e) {
            ConnStrModelOrigen.DataSource = txtServOrigen.Text;
            ConnStrModelOrigen.Catalog = txtBDOrigen.Text;
            if (chkSeguridadOrigen.IsChecked.HasValue ? chkSeguridadOrigen.IsChecked.Value : false) {
                ConnStrModelOrigen.IntSecurity = true;
                ConnStrModelOrigen.User = string.Empty;
                ConnStrModelOrigen.Passwd = string.Empty;
            } else {
                ConnStrModelOrigen.User = txtUsrOrigen.Text;
                ConnStrModelOrigen.Passwd = txtPassOrigen.Text;
            }
            txbConnStrOrigen.Text = "Cadena de conexión = " + ConnStrModelOrigen.ToString();

            ConnStrModelDestino.DataSource = txtServDestino.Text;
            ConnStrModelDestino.Catalog = txtBDDestino.Text;
            if (chkSeguridadDestino.IsChecked.HasValue ? chkSeguridadDestino.IsChecked.Value : false) {
                ConnStrModelDestino.IntSecurity = true;
                ConnStrModelDestino.User = string.Empty;
                ConnStrModelDestino.Passwd = string.Empty;
            } else {
                ConnStrModelDestino.User = txtUsrOrigen.Text;
                ConnStrModelDestino.Passwd = txtPassOrigen.Text;
            }
            txbConnStrDestino.Text = "Cadena de conexión = " + ConnStrModelDestino.ToString();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            XmlWriter writer = XmlWriter.Create(K_SRV_SETTINGS, settings);
            writer.WriteStartDocument();
            writer.WriteComment("This file is generated by the program.");
            writer.WriteStartElement("Servers");
            writer.WriteStartElement("Servidor");
            writer.WriteAttributeString("Tipo", "Origen");
            writer.WriteAttributeString("DataSource", ConnStrModelOrigen.DataSource);
            writer.WriteAttributeString("Catalog", ConnStrModelOrigen.Catalog);
            writer.WriteAttributeString("IntegratedSecurity", ConnStrModelOrigen.IntSecurity.ToString());
            writer.WriteAttributeString("User", ConnStrModelOrigen.IntSecurity ? string.Empty : ConnStrModelOrigen.User);
            writer.WriteEndElement();
            writer.WriteStartElement("Servidor");
            writer.WriteAttributeString("Tipo", "Destino");
            writer.WriteAttributeString("DataSource", ConnStrModelDestino.DataSource);
            writer.WriteAttributeString("Catalog", ConnStrModelDestino.Catalog);
            writer.WriteAttributeString("IntegratedSecurity", ConnStrModelDestino.IntSecurity.ToString());
            writer.WriteAttributeString("User", ConnStrModelDestino.IntSecurity ? string.Empty : ConnStrModelDestino.User);
            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();

        }

        private void btnCancealrServidores_Click(object sender, RoutedEventArgs e) {

        }

        private void btnOrigen_Click(object sender, RoutedEventArgs e) {
            using (Conn = new SqlConnection(ConnStrModelOrigen.ToString())) {
                using (Cmd = new SqlCommand() {
                    Connection = Conn,
                    CommandText = "",
                    CommandType = System.Data.CommandType.Text
                }) {

                }
            }
        }

        private void btnDestino_Click(object sender, RoutedEventArgs e) {

        }

        private void btnSincronizar_Click(object sender, RoutedEventArgs e) {

        }

        private void btnRevertirUsuario_Click(object sender, RoutedEventArgs e) {

        }
    }
}

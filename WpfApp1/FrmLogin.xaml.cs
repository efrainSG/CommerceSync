using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace WpfApp1 {
    /// <summary>
    /// Interaction logic for FrmLogin.xaml
    /// </summary>
    public partial class FrmLogin : Window {
        public UsuarioModel Usuario {
            get; set;
        }
        public FrmLogin() {
            InitializeComponent();
            txbMensaje.Text = string.Empty;
            Usuario = new UsuarioModel();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (Usuario.Usuario.Equals(txtUsuario.Text.ToLower()) &&
                Usuario.Password.Equals(txtPass.Password)) {
                this.DialogResult = true;
                this.Close();
            } else {
                //this.DialogResult = false;
                //this.Close();
                txbMensaje.Text = "Usuario y/o contraseña incorrectos.";
            }
        }

        private void txtUsuario_TextChanged(object sender, TextChangedEventArgs e) {
            txbMensaje.Text = string.Empty;
        }

        private void txtPass_PasswordChanged(object sender, RoutedEventArgs e) {
            txbMensaje.Text = string.Empty;
        }
    }
}


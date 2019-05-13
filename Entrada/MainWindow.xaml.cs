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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Threading;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Dsp;

using System.Diagnostics;


namespace Entrada
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WaveIn waveIn;
        DispatcherTimer timer;
        Stopwatch cronometro;
        string letraAnterior = "";
        string letraActual = "";
        
        float frecuenciaFundamental = 0.0f;

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = 
                TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            cronometro = new Stopwatch();
            LlenarComboDispositivos();

            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //Carro
            if (frecuenciaFundamental >= 500)
            {
                var leftCarro = Canvas.GetLeft(imgCarro);
                Canvas.SetLeft(imgCarro,
                    leftCarro +
                    (frecuenciaFundamental / 500.0) * 0.5);
            }
            else
            {
                Canvas.SetLeft(imgCarro, 10);
            }
            //Texto
            if (letraActual != "" &&
                letraActual == letraAnterior)
            {
                //Evaluar si ya pasó un segundo
                if(cronometro.ElapsedMilliseconds >=
                    1000)
                {
                    txtTexto.AppendText(letraActual);
                    letraActual = "";
                    cronometro.Restart();
                    if (txtTexto.Text.Length >= 2)
                    {
                        string texto  = txtTexto.Text.Substring(
                            txtTexto.Text.Length - 2, 2);
                        if (texto == "EO")
                        {
                            lblEO.Visibility = 
                                Visibility.Visible;

                        }
                    }
                }
            } else
            {
                cronometro.Restart();
            }
        }

        public void LlenarComboDispositivos()
        {
            for(int i=0; i<WaveIn.DeviceCount; 
                i++)
            {
                WaveInCapabilities capacidades =
                    WaveIn.GetCapabilities(i);
                cmbDispositivos.Items.Add(
                    capacidades.ProductName);
            }
            cmbDispositivos.SelectedIndex = 0;
        }

        private void btnIniciar_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            waveIn = new WaveIn();
            //Formato de audio
            waveIn.WaveFormat =
                new WaveFormat(44100, 16, 1);
            //Buffer
            waveIn.BufferMilliseconds =
                250;
            //¿Que hacer cuando hay muestras disponibles?
            waveIn.DataAvailable += WaveIn_DataAvailable;

            waveIn.StartRecording();
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int bytesGrabados = e.BytesRecorded;
            float acumulador = 0.0f;

            double numeroDeMuestras =
                bytesGrabados / 2;
            int exponente = 1;
            int numeroDeMuestrasComplejas = 0;
            int bitsMaximos = 0;

            do
            {
                bitsMaximos = (int)Math.Pow(2, exponente);
                exponente++;
            } while (bitsMaximos < numeroDeMuestras);

            numeroDeMuestrasComplejas = bitsMaximos / 2;
            exponente-=2;

            Complex[] señalCompleja =
                new Complex[numeroDeMuestrasComplejas];

            for(int i=0; i<bytesGrabados; i+=2)
            {
                //Transformando 2 bytes separados
                //en una muestra de 16 bits
                //1.- Toma el segundo byte y el antepone
                //     8 0's al principio
                //2.- Hace un OR con el primer byte, al cual
                //   automaticamente se le llenan 8 0's al final
                short muestra =
                    (short)(buffer[i + 1] << 8 | buffer[i]);
                float muestra32bits =
                    (float)muestra / 32768.0f;
                acumulador += Math.Abs(muestra32bits);

                if (i/2 < numeroDeMuestrasComplejas)
                {
                    señalCompleja[i / 2].X =
                        muestra32bits;
                }

            }
            float promedio = acumulador / 
                (bytesGrabados / 2.0f);
            sldMicrofono.Value = (double)promedio;

            //FastFourierTransform.FFT()

            if (promedio > 0)
            {
                FastFourierTransform.FFT(true, exponente, 
                    señalCompleja);

                float[] valoresAbsolutos =
                    new float[señalCompleja.Length];
                for(int i=0; i < señalCompleja.Length; i++)
                {
                    valoresAbsolutos[i] = (float)
                        Math.Sqrt(
                            (señalCompleja[i].X * señalCompleja[i].X) +
                            (señalCompleja[i].Y * señalCompleja[i].Y));
                }

                int indiceSeñalConMasPresencia =
                    valoresAbsolutos.ToList().IndexOf(
                        valoresAbsolutos.Max());

                frecuenciaFundamental =
                    (float)(indiceSeñalConMasPresencia *
                    waveIn.WaveFormat.SampleRate) /
                    (float)valoresAbsolutos.Length;
                letraAnterior = letraActual;
                if (frecuenciaFundamental >= 500 && 
                    frecuenciaFundamental <= 550)
                {
                    letraActual = "A";
                }else if (frecuenciaFundamental >= 600 &&
                    frecuenciaFundamental <= 650)
                {
                    letraActual = "E";
                }
                else if (frecuenciaFundamental >= 700 &&
                  frecuenciaFundamental <= 750)
                {
                    letraActual = "I";
                }
                else if (frecuenciaFundamental >= 800 &&
                  frecuenciaFundamental <= 850)
                {
                    letraActual = "O";
                }
                else if (frecuenciaFundamental >= 900 &&
                  frecuenciaFundamental <= 950)
                {
                    letraActual = "U";
                } else
                {
                    letraActual = "";
                }

                lblFrecuencia.Text =
                    frecuenciaFundamental.ToString("f");
                
            }


        }

        private void btnDetener_Click(object sender, RoutedEventArgs e)
        {
            waveIn.StopRecording();
        }
    }
}

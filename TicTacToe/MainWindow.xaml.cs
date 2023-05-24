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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net;
using System.Net.Sockets;

namespace TicTacToe
{
    public partial class MainWindow : Window
    {
        private UdpClient udpClient;
        private IPEndPoint serverEndPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string serverIP = ServerIPTextBox.Text;
            int serverPort;

            if (IPAddress.TryParse(serverIP, out IPAddress ipAddress) && int.TryParse(ServerPortTextBox.Text, out serverPort))
            {
                serverEndPoint = new IPEndPoint(ipAddress, serverPort);
                udpClient = new UdpClient();

                byte[] playerNameBytes = Encoding.ASCII.GetBytes(PlayerNameTextBox.Text);
                udpClient.Send(playerNameBytes, playerNameBytes.Length, serverEndPoint);

                GameWindow gameWindow = new GameWindow(udpClient, serverEndPoint);
                gameWindow.Show();

                Close();
            }
            else
            {
                MessageBox.Show("Invalid server IP or port.");
            }
        }
    }

    public enum PlayerSymbol
    {
        Cross,
        Circle,
        None
    }

    public enum GameStatus
    {
        NotStarted,
        InProgress,
        CrossWin,
        CircleWin,
        Draw
    }

    public partial class GameWindow : Window
    {
        private UdpClient udpClient;
        private IPEndPoint serverEndPoint;
        private Button[] cellButtons;
        private PlayerSymbol playerSymbol;

        public GameWindow(UdpClient client, IPEndPoint endPoint)
        {
            InitializeComponent();
            udpClient = client;
            serverEndPoint = endPoint;
            cellButtons = new Button[625];
            playerSymbol = PlayerSymbol.Circle;

            for (int i = 0; i < 625; i++)
            {
                cellButtons[i] = new Button();
                cellButtons[i].Content = "";
                cellButtons[i].Tag = i;
                cellButtons[i].Click += CellButton_Click;
                GameBoardGrid.Children.Add(cellButtons[i]);
            }

            UpdateGameBoard(new GameBoard());
            StartReceivingUpdates();
        }

        private void CellButton_Click(object sender, RoutedEventArgs e)
        {
            if (playerSymbol == PlayerSymbol.Circle)
            {
                Button button = (Button)sender;
                int cellIndex = (int)button.Tag;

                byte[] cellIndexBytes = BitConverter.GetBytes(cellIndex);
                udpClient.Send(cellIndexBytes, cellIndexBytes.Length, serverEndPoint);
            }
        }

        private void StartReceivingUpdates()
        {
            udpClient.BeginReceive(ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = null;
            byte[] receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);

            if (receivedBytes.Length == 626)
            {
                Dispatcher.Invoke(() =>
                {
                    GameStatus gameStatus = (GameStatus)receivedBytes[0];
                    int index = 1;

                    for (int i = 0; i < 2; i++)
                    {
                        string playerName = Encoding.ASCII.GetString(receivedBytes, index, 100).TrimEnd('\0');
                        index += 100;
                        SetPlayerName(i, playerName);
                    }

                    //for (int i = 0; i < 625; i++)
                    //{
                    //    SetCellState(i, (PlayerSymbol)receivedBytes[index++]);
                    //}

                    if (gameStatus == GameStatus.InProgress && playerSymbol == PlayerSymbol.Circle)
                        playerSymbol = PlayerSymbol.Cross;

                    if (gameStatus != GameStatus.NotStarted)
                    {
                        GameStatusLabel.Text = GetGameStatusLabel(gameStatus);
                        DisableCellButtons();
                    }
                });
            }

            StartReceivingUpdates();
        }

        private void SetPlayerName(int playerIndex, string playerName)
        {
            switch (playerIndex)
            {
                case 0:
                    Player1NameLabel.Text = playerName;
                    break;
                case 1:
                    Player2NameLabel.Text = playerName;
                    break;
            }
        }

        private void SetCellState(int cellIndex, PlayerSymbol symbol)
        {
            cellButtons[cellIndex].Content = symbol == PlayerSymbol.Cross ? "X" : "O";
            cellButtons[cellIndex].IsEnabled = symbol == PlayerSymbol.Cross ? false : true;
        }

        private void DisableCellButtons()
        {
            for (int i = 0; i < 625; i++)
            {
                cellButtons[i].IsEnabled = false;
            }
        }

        private string GetGameStatusLabel(GameStatus status)
        {
            switch (status)
            {
                case GameStatus.NotStarted:
                    return "Game not started.";
                case GameStatus.InProgress:
                    return "Game in progress.";
                case GameStatus.CrossWin:
                    return "Cross wins!";
                case GameStatus.CircleWin:
                    return "Circle wins!";
                case GameStatus.Draw:
                    return "It's a draw.";
                default:
                    return "";
            }
        }

        public void UpdateGameBoard(GameBoard gameBoard)
        {
            for (int i = 0; i < 625; i++)
            {
                SetCellState(i, gameBoard.GetCellState(i));
            }
        }
    }

    public class GameBoard
    {
        public const int Size = 25;
        private PlayerSymbol[] cells;

        public GameBoard()
        {
            cells = new PlayerSymbol[625];
        }

        public PlayerSymbol GetCellState(int index)
        {
            return cells[index];
        }

        public void SetCellState(int index, PlayerSymbol symbol)
        {
            cells[index] = symbol;
        }

        public bool IsCellEmpty(int index)
        {
            return cells[index] == PlayerSymbol.Cross || cells[index] == PlayerSymbol.Circle;
        }

        public bool IsBoardFull()
        {
            foreach (PlayerSymbol symbol in cells)
            {
                if (symbol == PlayerSymbol.None)
                    return false;
            }

            return true;
        }
    }
}
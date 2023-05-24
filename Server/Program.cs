using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TicTacToe;


namespace TicTacToeServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Tic-Tac-Toe Server");

                Server server = new Server();
                server.Start();

                Console.WriteLine("Для остановки сервера нажмите любую клавишу.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
    public class Server
    {
        private UdpClient udpServer;
        private IPEndPoint client1EndPoint;
        private IPEndPoint client2EndPoint;
        private Dictionary<IPEndPoint, string> playerNames;
        private Dictionary<IPEndPoint, PlayerSymbol> playerSymbols;
        private IPEndPoint currentPlayerEndPoint;
        private GameStatus gameStatus;
        private GameBoard gameBoard;
        private GameWindow gameWindow;

        public Server()
        {
            playerNames = new Dictionary<IPEndPoint, string>();
            playerSymbols = new Dictionary<IPEndPoint, PlayerSymbol>();
            gameBoard = new GameBoard();
            gameStatus = GameStatus.NotStarted;
        }

        public void Start()
        {
            udpServer = new UdpClient(new IPEndPoint(IPAddress.Any, 12345));
            Console.WriteLine("Server started. Waiting for players...");

            while (playerSymbols.Count < 2)
            {
                IPEndPoint clientEndPoint = null;
                byte[] receivedBytes = udpServer.Receive(ref clientEndPoint);
                string playerName = Encoding.ASCII.GetString(receivedBytes);

                if (!playerNames.ContainsKey(clientEndPoint))
                {
                    playerNames.Add(clientEndPoint, playerName);
                    playerSymbols.Add(clientEndPoint, playerSymbols.Count == 0 ? PlayerSymbol.Cross : PlayerSymbol.Circle);

                    if (playerSymbols.Count == 1)
                        client1EndPoint = clientEndPoint;
                    else
                        client2EndPoint = clientEndPoint;

                    Console.WriteLine("Player connected: " + playerName);
                }
            }

            gameStatus = GameStatus.InProgress;
            currentPlayerEndPoint = client1EndPoint;
            SendGameStateToClients();

            Console.WriteLine("Game started. Players: " + playerNames[client1EndPoint] + " (X) vs " + playerNames[client2EndPoint] + " (O)");

            while (gameStatus == GameStatus.InProgress)
            {
                IPEndPoint clientEndPoint = null;
                byte[] receivedBytes = udpServer.Receive(ref clientEndPoint);

                if (gameStatus == GameStatus.InProgress && currentPlayerEndPoint == clientEndPoint)
                {
                    int cellIndex = BitConverter.ToInt32(receivedBytes, 0);

                    if (gameBoard.IsCellEmpty(cellIndex))
                    {
                        gameBoard.SetCellState(cellIndex, playerSymbols[clientEndPoint]);

                        if (CheckWinCondition(playerSymbols[clientEndPoint]))
                        {
                            gameStatus = playerSymbols[clientEndPoint] == PlayerSymbol.Cross ? GameStatus.CrossWin : GameStatus.CircleWin;
                            SendGameStateToClients();
                            break;
                        }
                        else if (gameBoard.IsBoardFull())
                        {
                            gameStatus = GameStatus.Draw;
                            SendGameStateToClients();
                            break;
                        }
                        else
                        {
                            currentPlayerEndPoint = currentPlayerEndPoint == client1EndPoint ? client2EndPoint : client1EndPoint;
                            SendGameStateToClients();
                        }
                    }
                }
            }

            Console.WriteLine("Game over. " + (gameStatus == GameStatus.Draw ? "Draw." : "Winner: " + playerNames[currentPlayerEndPoint]));
            udpServer.Close();
           // gameWindow.Close();
        }

        private void SendGameStateToClients()
        {
            byte[] gameStateBytes = GetGameStateBytes();
            udpServer.Send(gameStateBytes, gameStateBytes.Length, client1EndPoint);
            udpServer.Send(gameStateBytes, gameStateBytes.Length, client2EndPoint);

            //if (gameWindow != null)
            //    gameWindow.UpdateGameBoard(gameBoard);
        }

        private byte[] GetGameStateBytes()
        {
            byte[] gameStateBytes = new byte[626];
            int index = 0;

            gameStateBytes[index++] = (byte)gameStatus;

            foreach (KeyValuePair<IPEndPoint, string> player in playerNames)
            {
                byte[] playerNameBytes = Encoding.ASCII.GetBytes(player.Value);
                Buffer.BlockCopy(playerNameBytes, 0, gameStateBytes, index, playerNameBytes.Length);
                index += 100;
            }

            foreach (KeyValuePair<IPEndPoint, PlayerSymbol> player in playerSymbols)
            {
                gameStateBytes[index++] = (byte)player.Value;
            }

            for (int i = 0; i < GameBoard.Size; i++)
            {
                gameStateBytes[index++] = (byte)gameBoard.GetCellState(i);
            }

            return gameStateBytes;
        }

        private bool CheckWinCondition(PlayerSymbol symbol)
        {
            // Check horizontal lines
            for (int row = 0; row < GameBoard.Size; row++)
            {
                for (int col = 0; col <= GameBoard.Size - 5; col++)
                {
                    bool win = true;

                    for (int i = 0; i < 5; i++)
                    {
                        if (gameBoard.GetCellState(row * GameBoard.Size + col + i) != symbol)
                        {
                            win = false;
                            break;
                        }
                    }

                    if (win)
                        return true;
                }
            }

            // Check vertical lines
            for (int col = 0; col < GameBoard.Size; col++)
            {
                for (int row = 0; row <= GameBoard.Size - 5; row++)
                {
                    bool win = true;

                    for (int i = 0; i < 5; i++)
                    {
                        if (gameBoard.GetCellState((row + i) * GameBoard.Size + col) != symbol)
                        {
                            win = false;
                            break;
                        }
                    }

                    if (win)
                        return true;
                }
            }

            // Check main diagonal lines
            for (int row = 0; row <= GameBoard.Size - 5; row++)
            {
                for (int col = 0; col <= GameBoard.Size - 5; col++)
                {
                    bool win = true;

                    for (int i = 0; i < 5; i++)
                    {
                        if (gameBoard.GetCellState((row + i) * GameBoard.Size + col + i) != symbol)
                        {
                            win = false;
                            break;
                        }
                    }

                    if (win)
                        return true;
                }
            }

            // Check anti-diagonal lines
            for (int row = 4; row < GameBoard.Size; row++)
            {
                for (int col = 0; col <= GameBoard.Size - 5; col++)
                {
                    bool win = true;

                    for (int i = 0; i < 5; i++)
                    {
                        if (gameBoard.GetCellState((row - i) * GameBoard.Size + col + i) != symbol)
                        {
                            win = false;
                            break;
                        }
                    }

                    if (win)
                        return true;
                }
            }

            return false;
        }
    }
}

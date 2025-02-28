using Hgnim.ConsoleWriter;
using LichessNET.API;
using LichessNET.Entities.Game;
using System;
using System.Text.Json.Nodes;

namespace LichessConsole
{
    internal class Program
    {
        static string? apiToken;
        static bool readKeyLoop = true;

        delegate void KeyAction(ConsoleKeyInfo cki);
        static event KeyAction? KeyInput;
        static void Main(string[] args) {
			if (args.Length == 1)
                apiToken = args[0];

            while (apiToken == null || apiToken=="") {
                Console.Write("Plase input API Token: ");
				apiToken=Console.ReadLine();
            }

			Task run = Task.Run(Run);
            while (readKeyLoop) {
                KeyInput?.Invoke(Console.ReadKey());
            }
            run.Wait();
        }
		static async void Run() {
			if (apiToken == null) return;
			var client = new LichessApiClient(apiToken);
			OngoingGame selectGame;
            {
                List<OngoingGame> ogames;
                ushort gamelistSelect = 0;

                try {
                    ogames = await client.GetOngoingGamesAsync();
                } catch (NullReferenceException nex) {
                    Console.WriteLine("Error: " + nex);
                    return;
                }

                Console.WriteLine("| 游戏ID 对手 类型 阵营 剩余时间 回合");
                CWriter gamelistCw = new();
                foreach (OngoingGame og in ogames) {
                    Console.Write("| ");
                    for (int i = 0; i < 5; i++) {
                        string opTxt = "";
                        switch (i) {
                            case 0:
                                opTxt = og.GameId; break;
                            case 1:
                                opTxt = og.Opponent.Username; break;
                            case 2:
                                opTxt = og.Speed; break;
                            case 3:
                                opTxt = og.Color;break;
                            case 4:
                                opTxt = TimeSpan.FromSeconds(og.SecondsLeft).ToString(@"hh\:mm\:ss"); break;
                        }
                        gamelistCw.Write(opTxt, ConsoleColor.White, ConsoleColor.DarkGray);
                        Console.Write(" ");
                    }
                    if (og.IsMyTurn)
                        gamelistCw.Write("你的回合", ConsoleColor.DarkGreen);
                    else
                        gamelistCw.Write("等待对手", ConsoleColor.DarkRed);
                    Console.WriteLine();
                }
                {
                    AutoResetEvent are = new(false);
                    void ListenKey(ConsoleKeyInfo cki) {
                        switch (cki.Key) {
                            case ConsoleKey.UpArrow:
                                if (gamelistSelect != 0)
                                    gamelistSelect--;
                                else
                                    gamelistSelect = (ushort)(ogames.Count - 1);
                                break;
                            case ConsoleKey.DownArrow:
                                if (gamelistSelect != ogames.Count - 1)
                                    gamelistSelect++;
                                else
                                    gamelistSelect = 0;
                                break;
                            case ConsoleKey.Enter:
                                gamelistCw.SetCursor(0, ogames.Count);
                                are.Set();
                                return;
                        }
                        for (int i = 0; i < ogames.Count; i++) {
                            if (i != gamelistSelect)
                                gamelistCw.LocWrite("|", 0, i);
                            else
                                gamelistCw.LocWrite(">", 0, i);
                        }
                    }
                    KeyInput += ListenKey;
                    are.WaitOne();
                    KeyInput -= ListenKey;
                }
                selectGame=ogames[gamelistSelect];
			}
            {
                CWriter gameCw = new();
                DrawLocationText(gameCw, selectGame.Color);
                DrawChess(gameCw, selectGame.Color, selectGame.Fen);
				GameStream gs= client.GetGameStreamAsync(selectGame.GameId).Result;
                gs.OnMoveMade += (sender, move) => {
					DrawChess(gameCw, selectGame.Color, );
				};
			}
		}
        static void DrawChess(CWriter cw,string color,string fen) {
			static ConsoleColor GetBackColor(int x,int y) => ((x + y) % 2 == 0) ? ConsoleColor.Yellow:ConsoleColor.DarkYellow;
            string[] chess = fen.Split(" ")[0].Split("/");
            switch (color) {
                case "white":
					for (int i = 0; i < 8; i++) {
                        byte xIndex = 0;
                        for (int j = 0; j < chess[i].Length; j++) {
                            if (chess[i][j] is >= '1' and <= '8') {
								for (int k = 0; k < int.Parse(chess[i][j].ToString()); k++) {
                                    cw.LocWrite(" ",
                                        xIndex/**2*/, i,
                                        backColor: GetBackColor(xIndex, i));
									xIndex++;
                                }
                            }
                            else {
                                cw.LocWrite(chess[i][j].ToString()/*.ToUpper()*/,
                                    xIndex/**2*/,i,
                                    char.IsUpper(chess[i][j])? ConsoleColor.Green: ConsoleColor.Red,
                                    GetBackColor(xIndex,i));
								xIndex++;
							}
						}
					}
                    break;
                case "black":
					for (int i = 0; i < 8; i++) {
						byte xIndex = 8-1;
						for (int j = 0; j < chess[i].Length; j++) {
							if (chess[i][j] is >= '1' and <= '8') {
								for (int k = 0; k < int.Parse(chess[i][j].ToString()); k++) {
									cw.LocWrite(" ",
										xIndex/**2*/, 8-1-i,
										backColor: GetBackColor(xIndex, i));
									xIndex--;
								}
							}
							else {
								cw.LocWrite(chess[i][j].ToString()/*.ToUpper()*/,
									xIndex/**2*/, 8 - 1 - i,
									char.IsUpper(chess[i][j]) ? ConsoleColor.Green : ConsoleColor.Red,
									GetBackColor(xIndex, i));
								xIndex--;
							}
						}
					}
					break;
            }
        }
        static void DrawLocationText(CWriter cw,string color) {
            switch (color) {
                case "white":
                    for(int i=0;i<8; i++) {
						cw.LocWrite(((char)('a' + i)).ToString(), i/**2*/, 8);
						cw.LocWrite(((char)('1' + i)).ToString(), 8/**2*/, 7 - i);
					}
                    break;
                case "black":
					for (int i = 0; i < 8; i++) {
						cw.LocWrite(((char)('h' - i)).ToString(), i/**2*/, 8);
						cw.LocWrite(((char)('8' - i)).ToString(), 8/**2*/, 7 - i);
					}
					break;
            }
        }
    }
}

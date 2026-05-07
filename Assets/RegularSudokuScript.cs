using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class RegularSudokuScript : MonoBehaviour
{
	public KMAudio Audio;
	public KMBombInfo Bomb;
	public KMBombModule Module;
	
	public GameObject[] Cubes;
	public GameObject StatusLight;
	public AudioClip SFX;
	
	int[,] m_sudoku = new int[9,9];
	int[,] m_subSquare = new int[,]
	{
		{0,0,0,1,1,1,2,2,2},
		{0,0,0,1,1,1,2,2,2},
		{0,0,0,1,1,1,2,2,2},
		{3,3,3,4,4,4,5,5,5},
		{3,3,3,4,4,4,5,5,5},
		{3,3,3,4,4,4,5,5,5},
		{6,6,6,7,7,7,8,8,8},
		{6,6,6,7,7,7,8,8,8},
		{6,6,6,7,7,7,8,8,8}
	};
	
	struct point
		{
			public int x,y;
			public point(int x, int y)
			{
				this.x = x;
				this.y = y;
			}
		}

	point[,] m_subIndex = new point[,]
	{
		{ new point(0,0),new point(0,1),new point(0,2),new point(1,0),new point(1,1),new point(1,2),new point(2,0),new point(2,1),new point(2,2)},
		{ new point(0,3),new point(0,4),new point(0,5),new point(1,3),new point(1,4),new point(1,5),new point(2,3),new point(2,4),new point(2,5)},
		{ new point(0,6),new point(0,7),new point(0,8),new point(1,6),new point(1,7),new point(1,8),new point(2,6),new point(2,7),new point(2,8)},
		{ new point(3,0),new point(3,1),new point(3,2),new point(4,0),new point(4,1),new point(4,2),new point(5,0),new point(5,1),new point(5,2)},
		{ new point(3,3),new point(3,4),new point(3,5),new point(4,3),new point(4,4),new point(4,5),new point(5,3),new point(5,4),new point(5,5)},
		{ new point(3,6),new point(3,7),new point(3,8),new point(4,6),new point(4,7),new point(4,8),new point(5,6),new point(5,7),new point(5,8)},
		{ new point(6,0),new point(6,1),new point(6,2),new point(7,0),new point(7,1),new point(7,2),new point(8,0),new point(8,1),new point(8,2)},
		{ new point(6,3),new point(6,4),new point(6,5),new point(7,3),new point(7,4),new point(7,5),new point(8,3),new point(8,4),new point(8,5)},
		{ new point(6,6),new point(6,7),new point(6,8),new point(7,6),new point(7,7),new point(7,8),new point(8,6),new point(8,7),new point(8,8)}
	};
	
	//Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	void Awake()
	{
		moduleId = moduleIdCounter++;
		for (int i = 0; i < Cubes.Length; i++)
		{
			int Press = i;
			Cubes[i].GetComponentInChildren<KMSelectable>().OnInteract += delegate ()
			{
				ButtonPress(Press);
				return false;
			};
		}
	}

	void Start()
	{
		Generate(33);
		string GenBoard = "", Answer = "";
		Debug.LogFormat("[Regular Sudoku #{0}] Initial board:", moduleId);
		for (int x = 0; x < 9; x++)
		{
			GenBoard = "";
			for (int y = 0; y < 9; y++)
			{
				GenBoard += m_sudoku[y,x] != 0 ? m_sudoku[y,x].ToString() : "*";
				Answer += m_sudoku[y,x];
				Cubes[x*9 + y].GetComponentInChildren<TextMesh>().text = m_sudoku[y,x] != 0 ? m_sudoku[y,x].ToString() : "";
				Cubes[x*9 + y].GetComponentInChildren<TextMesh>().color = m_sudoku[y,x] != 0 ? Color.black : new Color(100f/255f, 100f/255f, 100f/255f);
			}
			Debug.LogFormat("[Regular Sudoku #{0}] {1}", moduleId, GenBoard);
		}
		while (!Solve()) Solve();
		StartCoroutine(HideStatusLight());
		Debug.LogFormat("[Regular Sudoku #{0}] --------------------------------------------------------", moduleId);
		Debug.LogFormat("[Regular Sudoku #{0}] Answer:", moduleId);
		for (int x = 0; x < 9; x++)
		{
			Answer = "";
			for (int y = 0; y < 9; y++)
			{
				Answer += m_sudoku[y,x];
			}
			Debug.LogFormat("[Regular Sudoku #{0}] {1}", moduleId, Answer);
		}
	}
	
	void ButtonPress(int Press)
	{
		if (!ModuleSolved)
		{
			if (Cubes[Press].GetComponentInChildren<TextMesh>().color.r != 0f)
			{
				switch (Cubes[Press].GetComponentInChildren<TextMesh>().text)
				{
					case "":
						Cubes[Press].GetComponentInChildren<TextMesh>().text = "1";
						break;
					case "9":
						Cubes[Press].GetComponentInChildren<TextMesh>().text = "";
						break;
					default:
						Cubes[Press].GetComponentInChildren<TextMesh>().text = (Int32.Parse(Cubes[Press].GetComponentInChildren<TextMesh>().text) + 1).ToString();
						break;
				}
			}
			
			for (int x = 0; x < 3; x++)
			{
				switch (x)
				{
					case 0:
						for (int y = 0; y < 9; y++)
						{
							int Number = 0;
							for (int z = 0; z < 9; z++)
							{
								if (Cubes[y*9 + z].GetComponentInChildren<TextMesh>().text == "")
								{
									return;
								}
								Number += Int32.Parse(Cubes[y*9 + z].GetComponentInChildren<TextMesh>().text);
							}
							
							if (Number != 45)
							{
								return;
							}
						}
						break;
					case 1:
						for (int y = 0; y < 9; y++)
						{
							int Number = 0;
							for (int z = 0; z < 9; z++)
							{
								if (Cubes[z*9 + y].GetComponentInChildren<TextMesh>().text == "")
								{
									return;
								}
								Number += Int32.Parse(Cubes[z*9 + y].GetComponentInChildren<TextMesh>().text);
							}
							
							if (Number != 45)
							{
								return;
							}
						}
						break;
					case 2:
						for (int y = 0; y < 3; y++)
						{
							for (int z = 0; z < 3; z++)
							{
								int Number = 0;
								for (int a = 0; a < 3; a++)
								{
									for (int b = 0; b < 3; b++)
									{
										if (Cubes[((y*3)+a)*9 + ((z*3)+b)].GetComponentInChildren<TextMesh>().text == "")
										{
											return;
										}
										Number += Int32.Parse(Cubes[((y*3)+a)*9 + ((z*3)+b)].GetComponentInChildren<TextMesh>().text);
									}
								}
								
								if (Number != 45)
								{
									return;
								}
							}
						}
						break;
					default:
						break;
				}
			}
			
			/*for (int x = 0; x < 9; x++)
			{
				for (int y = 0; y < 9; y++)
				{
					if (Cubes[x*9 + y].GetComponentInChildren<TextMesh>().text != m_sudoku[y,x].ToString())
					{
						return;
					}
				}
			}
			*/
			
			for (int x = 0; x < 81; x++)
			{
				Cubes[x].GetComponentInChildren<TextMesh>().color = Cubes[x].GetComponentInChildren<TextMesh>().color.r != 0f ? new Color(24f/255f, 60f/255f, 0f) : Color.black;
			}
			
			Module.HandlePass();
			SendToSudokuCiphers(Enumerable.Range(0, 9)
				.SelectMany(y => Enumerable.Range(0, 9).Select(x => m_sudoku[x, y]))
				.ToArray());
			ModuleSolved = true;
			Audio.PlaySoundAtTransform(SFX.name, transform);
		}
	}
	
	private IEnumerator HideStatusLight()
    {
        yield return null;
        StatusLight.transform.localScale = new Vector3(0, 0, 0);
    }
	
	bool Solve()
	{
		int xp = 0;
		int yp = 0;
		byte[]	Mp	=	null;
		int cMp = 10;
		for(int y = 0; y < 9; y++)
		{
			for(int x = 0; x < 9; x++)
			{
				if(m_sudoku[y,x] == 0)
				{
					byte[] M = {0,1,2,3,4,5,6,7,8,9};
					
					for(int a = 0; a < 9; a++)
						M[m_sudoku[a,x]] = 0;

					for(int b = 0; b < 9; b++)
						M[m_sudoku[y,b]] = 0;

					int	squareIndex = m_subSquare[y,x];
					for(int c = 0; c < 9; c++)
					{
						point p = m_subIndex[squareIndex,c];
						M[m_sudoku[p.x,p.y]] = 0;
					}

					int cM = 0;
					for(int d = 1; d < 10; d++)
						cM += M[d] == 0 ? 0 : 1;

					if(cM < cMp)
					{
						cMp = cM;
						Mp = M;
						xp = x;
						yp = y;
					}
				}
			}
		}

		if(cMp == 10)
			return true;
		if(cMp == 0)
			return false;

		for(int i = 1; i < 10; i++)
		{
			if(Mp[i] != 0)
			{
				m_sudoku[yp,xp] = Mp[i];
				if(Solve())
					return true;
			}
		}

		m_sudoku[yp,xp] = 0;
		return false;
	}
	
	bool Generate(int spots)
	{
		int num = GetNumberSpots();

		if(!IsSudokuFeasible() || num > spots)
		{
			Clear();
			num = 0;
		}
		do
		{
			if(Gen(spots - num))
			{
				if(IsSudokuUnique())
				{
					return true;
				}
			}

			Clear();
			num = 0;
		} while(true);
	}

	int[,] Data
	{
		get
		{
			return m_sudoku;
		}

		set
		{
			if(value.Rank == 2 && value.GetUpperBound(0) == 8 && value.GetUpperBound(1) == 8)
				m_sudoku = value;
			else
				throw new Exception("Array has wrong size");
		}
	}

	bool IsSudokuFeasible()
	{
		for(int y = 0; y < 9; y++)
		{
			for(int x = 0; x < 9; x++)
			{
				int[] M = new int[10];

				for(int a = 0; a < 9; a++)
					M[m_sudoku[a,x]]++;
				if(!Feasible(M))
					return false;

				M = new int[10];
				for(int b = 0; b < 9; b++)
					M[m_sudoku[y,b]]++;
				if(!Feasible(M))
					return false;

				M = new int[10];
				int	squareIndex = m_subSquare[y,x];
				for(int c = 0; c < 9; c++)
				{
					point p = m_subIndex[squareIndex,c];
					if(p.x != y && p.y != x)
						M[m_sudoku[p.x,p.y]]++;
				}
				if(!Feasible(M))
					return false;
			}
		}

		return true;
	}

	bool IsSudokuUnique()
	{
		int[,] m = GetCopy();
		int u = TestUniqueness();
		Data = m;
		return u == 1;
	}

	bool Feasible(int[] M)
	{
		for(int d = 1; d < 10; d++)
			if(M[d] > 1) 
				return false;

		return true;
	}

	int[,] GetCopy()
	{
		int[,] copy = new int[9,9];
		for(int x = 0; x < 9; x++)
			for(int y = 0; y < 9; y++)
				copy[y,x] = m_sudoku[y,x];

		return copy;
	}

	void Clear()
	{
		for(int y = 0; y < 9; y++)
			for(int x = 0; x < 9; x++)
				m_sudoku[y,x] = 0;
	}

	int GetNumberSpots()
	{
		int num = 0;

		for(int y = 0; y < 9; y++)
			for(int x = 0; x < 9; x++)
				num += m_sudoku[y,x]==0 ? 0:1;

		return num;
	}

	// Generate spots
	bool Gen(int spots)
	{
		for(int i = 0;  i < spots; i++)
		{
			int xRand,yRand;
			do
			{
				xRand = UnityEngine.Random.Range(0, 9);
				yRand = UnityEngine.Random.Range(0, 9);
			} while(m_sudoku[yRand,xRand] != 0);

			int[] M = {0,1,2,3,4,5,6,7,8,9};

			for(int a = 0; a < 9; a++)
				M[m_sudoku[a,xRand]] = 0;

			for(int b = 0; b < 9; b++)
				M[m_sudoku[yRand,b]] = 0;

			int	squareIndex = m_subSquare[yRand,xRand];
			for(int c = 0; c < 9; c++)
			{
				point p = m_subIndex[squareIndex,c];
				M[m_sudoku[p.x,p.y]] = 0;
			}

			int cM = 0;
			for(int d = 1; d < 10; d++)
				cM += M[d] == 0 ? 0 : 1;

			if(cM > 0)
			{
				int e = 0;

				do
				{
					e =  UnityEngine.Random.Range(1,10);
				} while(M[e] == 0);

				m_sudoku[yRand,xRand] = (int)e;
			}
			else
			{
				return false;
			}
		}

		return true;
	}

	// Returns 0 if there are no solutions,
	// 1 if there is only one solution (unique puzzle),
	// or 2 if there are multiple solutions
	int TestUniqueness()
	{
		int xp = 0;
		int yp = 0;
		int[]	Mp	=	null;
		int cMp = 10;

		for(int y = 0; y < 9; y++)
		{
			for(int x = 0; x < 9; x++)
			{
				if(m_sudoku[y,x] == 0)
				{
					int[] M = {0,1,2,3,4,5,6,7,8,9};

					for(int a = 0; a < 9; a++)
						M[m_sudoku[a,x]] = 0;

					for(int b = 0; b < 9; b++)
						M[m_sudoku[y,b]] = 0;

					int	squareIndex = m_subSquare[y,x];
					for(int c = 0; c < 9; c++)
					{
						point p = m_subIndex[squareIndex,c];
						M[m_sudoku[p.x,p.y]] = 0;
					}

					int cM = 0;
					for(int d = 1; d < 10; d++)
						cM += M[d] == 0 ? 0 : 1;

					if(cM < cMp)
					{
						cMp = cM;
						Mp = M;
						xp = x;
						yp = y;
					}
				}
			}
		}

		if(cMp == 10)
			return 1;

		if(cMp == 0)
			return 0;

		int success = 0;
		for(int i = 1; i < 10; i++)
		{
			if(Mp[i] != 0)
			{
				m_sudoku[yp,xp] = Mp[i];
				success += TestUniqueness();

				m_sudoku[yp,xp] = 0;

				if(success > 1)
					return 2;
			}
		}

		return success;
	}

	// sudoku cipher - uses reflection to construct data and send solved grid to the cipher module
	void SendToSudokuCiphers(int[] sudokuSolution)
	{
		var ciphers = FindObjectsOfType<MonoBehaviour>()
			.Where(mb => mb.GetType().Name == "SudokuCipher");

		var regularDataType = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetSafeTypes())
			.FirstOrDefault(t => t.Name == "RegularSudokuData");

		if (regularDataType == null)
			return;
			
		foreach (var cipher in ciphers)
		{
			if (cipher.transform.parent != transform.parent)
				continue;
			var sudokuDataInstance = Activator.CreateInstance(regularDataType);
			var listInstance = sudokuSolution.ToList();
			sudokuDataInstance.SetValue("solution", listInstance);
			try { cipher.CallMethod("Enqueue", "Regular", sudokuDataInstance); }
			catch (Exception e) { Debug.LogFormat("[Regular Sudoku{0}] {1}", moduleId, e); }
		}
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To set a certain coordinate with a certain value, use the command !{0} set [A-I][1-9] <1-9 / blank>. Multiple coordinates may be set, for example: !{0} set a1 1 b1 2 | To set all tiles in reading order, use the command !{0} set all [81 VALUES] where '*' is blank | To clear all the inputs on the board, use the command !{0} full reset";
    #pragma warning restore 414
	
	string[] CoordinatesL = {"A", "B", "C", "D", "E", "F", "G", "H", "I"};
	string[] CoordinatesN = {"1", "2", "3", "4", "5", "6", "7", "8", "9"};
	
	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] parameters = command.ToLower().Split(' ');
		
		if (Regex.IsMatch(parameters[0], @"^\s*set\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (parameters.Length % 2 != 1 || parameters.Length == 1)
			{
				yield return "sendtochaterror Invalid parameter length. Command ignored.";
				yield break;
			}

			if (parameters[1].Equals("all"))
            {
				if (parameters.Length != 3)
				{
					yield return "sendtochaterror Invalid parameter length. Command ignored.";
					yield break;
				}

				if (parameters[2].Length != 81)
                {
					yield return "sendtochaterror 81 values were not specified. Command ignored.";
					yield break;
				}

				for (int x = 0; x < 9; x++)
				{
					for (int y = 0; y < 9; y++)
					{
						if (Cubes[x * 9 + y].GetComponentInChildren<TextMesh>().color.r == 0f && !Cubes[x * 9 + y].GetComponentInChildren<TextMesh>().text.Equals(parameters[2][x * 9 + y].ToString()))
						{
							yield return "sendtochaterror A starting clue was not set with its value. Command ignored.";
							yield break;
						}

						if (Cubes[x * 9 + y].GetComponentInChildren<TextMesh>().color.r != 0f && !CoordinatesN.Contains(parameters[2][x * 9 + y].ToString()) && (parameters[2][x * 9 + y].ToString() != "*"))
						{
							yield return "sendtochaterror Invalid value detected. Command ignored.";
							yield break;
						}
					}
				}

				for (int x = 0; x < 9; x++)
				{
					for (int y = 0; y < 9; y++)
					{
						if (parameters[2][x * 9 + y].ToString() == "*")
                        {
							while (!Cubes[x * 9 + y].GetComponentInChildren<TextMesh>().text.Equals(""))
							{
								Cubes[x * 9 + y].GetComponentInChildren<KMSelectable>().OnInteract();
								yield return new WaitForSecondsRealtime(0.025f);
							}
						}
                        else
                        {
							while (!Cubes[x * 9 + y].GetComponentInChildren<TextMesh>().text.Equals(parameters[2][x * 9 + y].ToString()))
							{
								Cubes[x * 9 + y].GetComponentInChildren<KMSelectable>().OnInteract();
								yield return new WaitForSecondsRealtime(0.025f);
							}
						}
					}
				}
			}
            else
            {
				for (int i = 1; i < parameters.Length; i += 2)
				{
					if (parameters[i].Length != 2 || !parameters[i][0].ToString().ToUpper().EqualsAny(CoordinatesL) || !parameters[i][1].ToString().EqualsAny(CoordinatesN))
					{
						yield return "sendtochaterror Invalid coordinate detected. Command ignored.";
						yield break;
					}

					if (Cubes[(Array.IndexOf(CoordinatesN, parameters[i][1].ToString()) * 9) % 81 + Array.IndexOf(CoordinatesL, parameters[i][0].ToString().ToUpper())].GetComponentInChildren<TextMesh>().color.r == 0f)
					{
						yield return "sendtochaterror A coordinate being changed is a starting clue. Command ignored.";
						yield break;
					}

					if (parameters[i + 1].ToLower() != "blank" && !parameters[i + 1].EqualsAny(CoordinatesN))
					{
						yield return "sendtochaterror A coordinate is being set with an invalid value. Command ignored.";
						yield break;
					}
				}

				for (int i = 1; i < parameters.Length; i += 2)
				{
					switch (parameters[i + 1].ToLower())
					{
						case "blank":
							while (Cubes[(Array.IndexOf(CoordinatesN, parameters[i][1].ToString()) * 9) % 81 + Array.IndexOf(CoordinatesL, parameters[i][0].ToString().ToUpper())].GetComponentInChildren<TextMesh>().text != "")
							{
								Cubes[(Array.IndexOf(CoordinatesN, parameters[i][1].ToString()) * 9) % 81 + Array.IndexOf(CoordinatesL, parameters[i][0].ToString().ToUpper())].GetComponentInChildren<KMSelectable>().OnInteract();
								yield return new WaitForSecondsRealtime(0.025f);
							}
							break;
						default:
							while (Cubes[(Array.IndexOf(CoordinatesN, parameters[i][1].ToString()) * 9) % 81 + Array.IndexOf(CoordinatesL, parameters[i][0].ToString().ToUpper())].GetComponentInChildren<TextMesh>().text != parameters[i + 1].ToLower())
							{
								Cubes[(Array.IndexOf(CoordinatesN, parameters[i][1].ToString()) * 9) % 81 + Array.IndexOf(CoordinatesL, parameters[i][0].ToString().ToUpper())].GetComponentInChildren<KMSelectable>().OnInteract();
								yield return new WaitForSecondsRealtime(0.025f);
							}
							break;
					}
				}
			}
		}
		
		if (Regex.IsMatch(command, @"^\s*full reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			for (int x = 0; x < 9; x++)
			{
				for (int y = 0; y < 9; y++)
				{
					Cubes[x*9 + y].GetComponentInChildren<TextMesh>().text = Cubes[x*9 + y].GetComponentInChildren<TextMesh>().color.r != 0f ? "" : Cubes[x*9 + y].GetComponentInChildren<TextMesh>().text;
				}
			}
		}
	}

	IEnumerator TwitchHandleForcedSolve()
    {
		for (int x = 0; x < 9; x++)
		{
			for (int y = 0; y < 9; y++)
			{
				while (Cubes[x * 9 + y].GetComponentInChildren<TextMesh>().text != m_sudoku[y,x].ToString())
                {
					Cubes[x*9 + y].GetComponentInChildren<KMSelectable>().OnInteract();
					yield return new WaitForSecondsRealtime(0.025f);
                }
			}
		}
	}
}

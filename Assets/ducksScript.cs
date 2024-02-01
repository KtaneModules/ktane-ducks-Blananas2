using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ducksScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable[] Ducks; //Up, Right, Down, Left
    public GameObject[] DuckObjs;
    public Material[] DuckMats; //Blue, Red, Green, Yellow
    public GameObject PoolObj;
    public Renderer Walter;

    int[][] Room = new int[][] {
        new int[] {2,0,3,1}, new int[] {1,0,2,3}, new int[] {2,1,0,3}, new int[] {3,1,2,0}, new int[] {0,1,2,3},
        new int[] {2,1,3,0}, new int[] {2,3,0,1}, new int[] {1,3,0,2}, new int[] {3,2,1,0}, new int[] {3,2,0,1},
        new int[] {3,0,2,1}, new int[] {0,3,2,1}, new int[] {9,9,9,9}, new int[] {3,1,0,2}, new int[] {2,0,1,3},
        new int[] {2,3,1,0}, new int[] {0,3,1,2}, new int[] {1,3,2,0}, new int[] {1,2,0,3}, new int[] {0,2,1,3},
        new int[] {0,1,3,2}, new int[] {1,2,3,0}, new int[] {3,0,1,2}, new int[] {0,2,3,1}, new int[] {1,0,3,2}
    };
    int Position = -1;
    int Orientation = -1; //Which duck is on top?
    bool ValidMovement = true;
    int Pressed = -1;
    int[] Squeaks = { -1, -1, -1, -1 };
    string[] DirectionNames = { "Up", "Right", "Down", "Left" };

    //Logging
    static int ModuleIDCounter = 1;
    int ModuleID;
    private bool ModuleSolved;

    void Awake () {
        ModuleID = ModuleIDCounter++;

        foreach (KMSelectable Duck in Ducks) {
            Duck.OnInteract += delegate () { DuckPress(Duck); return false; };
            Duck.OnInteractEnded += delegate () { DuckRelease(); };
        }
    }

    // Use this for initialization
    void Start () {
        do { Position = Rnd.Range(0, 25); } 
            while (Position == 12);
        Orientation = Rnd.Range(0, 4);

        Debug.LogFormat("[Ducks #{0}] You are at {1} with {2} at the top.", ModuleID, Coord(Position), DirectionNames[Orientation]);
        Color();

        for (int S = 0; S < 4; S++) {
            do { Squeaks[S] = Rnd.Range(0, 10); }
                while ( Squeaks[S] == Squeaks[(S + 1) % 4] || Squeaks[S] == Squeaks[(S + 2) % 4] || Squeaks[S] == Squeaks[(S + 3) % 4]);
        }
    }

    void Color () {
        for (int J = 0; J < 4; J++) {
            DuckObjs[J].GetComponent<MeshRenderer>().material = DuckMats[Room[Position][(J + Orientation) % 4]];
        }
    }

    string Coord (int p) {
        string alpha = "ABCDE";
        return alpha[p%5] + ((p/5)+1).ToString();
    }

    void DuckPress(KMSelectable Duck) {
        for (int D = 0; D < 4; D++) {
            if (Duck == Ducks[D]) {
                Pressed = D;

                Audio.PlaySoundAtTransform("squeak-a" + Squeaks[D], transform);
                DuckObjs[D].transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                if (ModuleSolved) { return; }

                int Direction = (D + Orientation) % 4;
                switch (Direction) {
                    case 0: 
                        if (Position / 5 == 0) { ValidMovement = false; }
                        else { Position -= 5; }
                    break;
                    case 1: 
                        if (Position % 5 == 4) { ValidMovement = false; } 
                        else { Position += 1; }
                    break;
                    case 2: 
                        if (Position / 5 == 4) { ValidMovement = false; } 
                        else { Position += 5; }
                    break;
                    case 3: 
                        if (Position % 5 == 0) { ValidMovement = false; } 
                        else { Position -= 1; }
                    break;
                }
                Debug.LogFormat("[Ducks #{0}] Squeezed {1}{2}.", ModuleID, DirectionNames[D], ValidMovement ? (", you're now at " + Coord(Position)) : "");
            }
        }
    }

    void DuckRelease() {
        Audio.PlaySoundAtTransform("squeak-b" + Squeaks[Pressed], transform);
        DuckObjs[Pressed].transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
        if (ModuleSolved) { return; }

        if (!ValidMovement) {
            Debug.LogFormat("[Ducks #{0}] You hit a wall, strike!", ModuleID);
            GetComponent<KMBombModule>().HandleStrike();
            ValidMovement = true;
        } else if (Position == 12) {
            Debug.LogFormat("[Ducks #{0}] You made it to the pool, module solved.", ModuleID);
            GetComponent<KMBombModule>().HandlePass();
            ModuleSolved = true;
            PoolObj.SetActive(true);
            for (int d = 0; d < 4; d++) {
                DuckObjs[d].SetActive(false);
            }
            StartCoroutine(WalterWhite());
        } else {
            Color();
        }
    }

    IEnumerator WalterWhite() {
        float WaltX = Rnd.Range(0, 589) * 0.0017f;
        float WaltY = Rnd.Range(0, 589) * 0.0017f;
        while (true) {
            WaltX += 0.001f;
            WaltY += 0.001f;
            Walter.material.mainTextureOffset = new Vector2(WaltX, WaltY);
            yield return new WaitForSeconds(0.025f);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press URDL [Press the duck Up, Right, Down, then Left]"; //Duck KMSelectables are in this order
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim();
        if (Regex.IsMatch(command, @"^press\s+[UDLR]+$", RegexOptions.IgnoreCase))
        {
            yield return null;
            int[] directions = command.Split(' ').Where(s => !string.IsNullOrEmpty(s)).Last().ToUpper().ToCharArray().Select(dir =>
            {
                switch (dir)
                {
                    case 'U': return 0;
                    case 'R': return 1;
                    case 'D': return 2;
                    case 'L': return 3;
                    default: throw new ArgumentException("Not a direction.");
                }
            }).ToArray();
            foreach (int direction in directions)
            {
                yield return PressDuckie(Ducks[direction]);
                yield return new WaitForSecondsRealtime(.25f);
            }
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!ModuleSolved)
        {
            int y = Position / 5, x = Position % 5;
            if (x < 2)
            {
                yield return PressDuckie(Ducks[ActualModulo(1 - Orientation, 4)]);
                yield return new WaitForSecondsRealtime(.25f);
            }
            else if (x > 2)
            {
                yield return PressDuckie(Ducks[ActualModulo(3 - Orientation, 4)]);
                yield return new WaitForSecondsRealtime(.25f);
            }
            if (y < 2)
            {
                yield return PressDuckie(Ducks[ActualModulo(2 - Orientation, 4)]);
                yield return new WaitForSecondsRealtime(.25f);
            }
            else if (y > 2)
            {
                yield return PressDuckie(Ducks[ActualModulo(4 - Orientation, 4)]);
                yield return new WaitForSecondsRealtime(.25f);
            }
        }
    }

    private IEnumerator PressDuckie(KMSelectable duckie)
    {
        duckie.OnInteract();
        yield return new WaitForSecondsRealtime(.1f);
        duckie.OnInteractEnded();
    }

    private int ActualModulo(int n, int m)
    {
        return (n % m + m) % m;
    }
}

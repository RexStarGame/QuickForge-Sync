using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace exam_test
{
    public partial class Form1 : Form
    {
        private string axiom = "";
        private Dictionary<char, string> rules = new Dictionary<char, string>();

        private float angle;
        private float lineLength;
        private int iterations;

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;

            SetupKochSnowflake();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Empty method. It does not hurt to keep it here.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            string instructions = GenerateLSystem(axiom, rules, iterations);

            TurtleState startState = new TurtleState
            {
                Position = new PointF(150, 400),
                Angle = 0
            };

            Stack<TurtleState> savedStates = new Stack<TurtleState>();

            DrawInstructionsRecursive(
                e.Graphics,
                instructions,
                0,
                ref startState,
                savedStates
            );
        }

        private void SetupKochCurve()
        {
            axiom = "F";

            rules = new Dictionary<char, string>
            {
                { 'F', "F+F--F+F" }
            };

            angle = 60;
            lineLength = 5;
            iterations = 4;
        }

        private void SetupKochSnowflake()
        {
            axiom = "F--F--F";

            rules = new Dictionary<char, string>
            {
                { 'F', "F+F--F+F" }
            };

            angle = 60;
            lineLength = 4;
            iterations = 4;
        }

        private void SetupSierpinskiArrowhead()
        {
            axiom = "F";

            rules = new Dictionary<char, string>
            {
                { 'F', "G-F-G" },
                { 'G', "F+G+F" }
            };

            angle = 60;
            lineLength = 6;
            iterations = 5;
        }

        private void SetupPlant()
        {
            axiom = "X";

            rules = new Dictionary<char, string>
            {
                { 'F', "FF" },
                { 'X', "-F[+F][--X]+F+F[++++X]-X" }
            };

            angle = 10;
            lineLength = 5;
            iterations = 4;
        }

        private string GenerateLSystem(string current, Dictionary<char, string> rules, int remainingIterations)
        {
            if (remainingIterations <= 0)
            {
                return current;
            }

            string next = ApplyRulesRecursive(current, rules);

            return GenerateLSystem(next, rules, remainingIterations - 1);
        }

        private string ApplyRulesRecursive(string input, Dictionary<char, string> rules, int index = 0)
        {
            if (index >= input.Length)
            {
                return "";
            }

            char currentChar = input[index];

            string replacement;

            if (rules.TryGetValue(currentChar, out string? ruleReplacement))
            {
                replacement = ruleReplacement;
            }
            else
            {
                replacement = currentChar.ToString();
            }

            return replacement + ApplyRulesRecursive(input, rules, index + 1);
        }

        private void DrawInstructionsRecursive(
            Graphics g,
            string instructions,
            int index,
            ref TurtleState state,
            Stack<TurtleState> savedStates)
        {
            if (index >= instructions.Length)
            {
                return;
            }

            char command = instructions[index];

            if (command == 'F' || command == 'G')
            {
                DrawForward(g, ref state);
            }
            else if (command == '+')
            {
                state.Angle += angle;
            }
            else if (command == '-')
            {
                state.Angle -= angle;
            }
            else if (command == '[')
            {
                savedStates.Push(state);
            }
            else if (command == ']')
            {
                if (savedStates.Count > 0)
                {
                    state = savedStates.Pop();
                }
            }

            DrawInstructionsRecursive(g, instructions, index + 1, ref state, savedStates);
        }

        private void DrawForward(Graphics g, ref TurtleState state)
        {
            float radians = state.Angle * (float)Math.PI / 180;

            float newX = state.Position.X + lineLength * (float)Math.Cos(radians);
            float newY = state.Position.Y + lineLength * (float)Math.Sin(radians);

            PointF newPosition = new PointF(newX, newY);

            g.DrawLine(Pens.Black, state.Position, newPosition);

            state.Position = newPosition;
        }
    }

    public struct TurtleState
    {
        public PointF Position;
        public float Angle;
    }
}
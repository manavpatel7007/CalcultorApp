using System.Globalization;

namespace CalculatorApp
{
    public partial class MainPage : ContentPage
    {
        
        string _current = "0"; // current entry as string
        double _accumulator = 0; // running total
        string? _pendingOp = null; // canonical: "+", "-", "*", "/"
        bool _resetOnNextDigit = false; // after op/equals, next digit replaces display


        readonly CultureInfo _ci = CultureInfo.InvariantCulture;

        public MainPage()
        {
            InitializeComponent();
            UpdateDisplay();
            SetOpActive(null);
        }
        void UpdateDisplay(string? expr = null)
        {
            DisplayLabel.Text = FormatNumber(_current);
            ExprLabel.Text = expr ?? BuildExpressionPreview();
            BtnClear.Text = (_current != "0" || _pendingOp != null) ? "C" : "AC";
        }


        string BuildExpressionPreview()
        {
            if (_pendingOp is null) return string.Empty;
            return $"{FormatNumber(_accumulator)} {DisplayOp(_pendingOp)}";
        }
        static string FormatNumber(string s)
        {
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return s;
            return v.ToString("G15", CultureInfo.CurrentCulture);
        }
        static string FormatNumber(double v)
        {
            return v.ToString("G15", CultureInfo.CurrentCulture);
        }

        // DIGITS
        void OnDigit(object sender, EventArgs e)
        {
            var d = ((Button)sender).Text;
            if (_resetOnNextDigit || _current == "0")
            {
                _current = d;
                _resetOnNextDigit = false;
            }
            else
            {
                _current += d;
            }
            UpdateDisplay();
        }
        // DECIMAL
        void OnDecimal(object sender, EventArgs e)
        {
            if (_resetOnNextDigit)
            {
                _current = "0";
                _resetOnNextDigit = false;
            }
            if (!_current.Contains('.')) _current += ".";
            UpdateDisplay();
        }


        // OPERATORS
        void OnOperator(object sender, EventArgs e)
        {
            var opLabel = ((Button)sender).Text; // + − × ÷ or ASCII
            var op = CanonicalOp(opLabel);


            // If user presses operator twice, switch it
            if (_pendingOp is not null && _resetOnNextDigit)
            {
                _pendingOp = op;
                SetOpActive(op);
                UpdateDisplay();
                return;
            }


            var expr = ApplyPending(); // compute prior op if any
            _pendingOp = op;
            _resetOnNextDigit = true;
            SetOpActive(op);
            UpdateDisplay(expr); // show "acc op cur" while chaining
        }

        // EQUALS
        void OnEquals(object sender, EventArgs e)
        {
            var expr = ApplyPending();
            _pendingOp = null;
            _resetOnNextDigit = true;
            SetOpActive(null);
            UpdateDisplay(expr is null ? null : expr + " =");
        }


        // CLEAR: iPhone-like — C clears entry, AC clears all
        void OnClear(object sender, EventArgs e)
        {
            if (_current != "0")
            {
                _current = "0"; // clear entry
                _resetOnNextDigit = false;
            }
            else
            {
                _accumulator = 0; // clear all
                _pendingOp = null;
                _resetOnNextDigit = false;
                SetOpActive(null);
            }
            UpdateDisplay();
        }
        // SIGN TOGGLE
        void OnToggleSign(object sender, EventArgs e)
        {
            if (_current.StartsWith("-")) _current = _current.Substring(1);
            else if (_current != "0") _current = "-" + _current;
            UpdateDisplay();
        }


        // PERCENT (simple: divide by 100)
        void OnPercent(object sender, EventArgs e)
        {
            if (double.TryParse(_current, NumberStyles.Float, _ci, out var v))
            {
                v = v / 100.0;
                _current = v.ToString(_ci);
                UpdateDisplay();
            }
        }
        // RECIPROCAL 1/x (applies to current entry)
        void OnReciprocal(object sender, EventArgs e)
        {
            if (double.TryParse(_current, NumberStyles.Float, _ci, out var cur))
            {
                if (cur == 0)
                {
                    _current = "0"; // keep simple; could show error
                }
                else
                {
                    _current = (1.0 / cur).ToString(_ci);
                }
                _resetOnNextDigit = true;
                UpdateDisplay();
            }
        }

        // Compute pending op; return expression string used (acc op cur) if an op applied
        string? ApplyPending()
        {
            if (!double.TryParse(_current, NumberStyles.Float, _ci, out var cur))
                return null;


            if (_pendingOp is null)
            {
                _accumulator = cur;
                return null;
            }


            var prev = _accumulator;
            _accumulator = _pendingOp switch
            {
                "+" => _accumulator + cur,
                "-" => _accumulator - cur,
                "*" => _accumulator * cur,
                "/" => cur == 0 ? 0 : _accumulator / cur,
                _ => _accumulator
            };
            _current = _accumulator.ToString(_ci);
            return $"{FormatNumber(prev.ToString(_ci))} {DisplayOp(_pendingOp)} {FormatNumber(cur.ToString(_ci))}";
        }
        string CanonicalOp(string label) => label switch
        {
            "×" or "x" or "X" or "*" => "*",
            "÷" or "/" => "/",
            "−" or "-" => "-",
            "+" => "+",
            _ => label
        };


        string DisplayOp(string canonical) => canonical switch
        {
            "*" => "×",
            "/" => "÷",
            "-" => "−",
            "+" => "+",
            _ => canonical
        };
        void SetOpActive(string? canonical)
        {
            var opColor = (Color)Application.Current!.Resources["Op"];
            var active = (Color)Application.Current!.Resources["OpActive"];


            BtnAdd.BackgroundColor = canonical == "+" ? active : opColor;
            BtnSub.BackgroundColor = canonical == "-" ? active : opColor;
            BtnMul.BackgroundColor = canonical == "*" ? active : opColor;
            BtnDiv.BackgroundColor = canonical == "/" ? active : opColor;
        }
    }
}

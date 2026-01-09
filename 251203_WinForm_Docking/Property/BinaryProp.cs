using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using _251203_WinForm_Docking.Core;
using _251203_WinForm_Docking.Algorithm;

namespace _251203_WinForm_Docking.Property
{
    public enum ShowBinaryMode : int
    {
        ShowBinaryNone = 0,
        ShowBinaryHighlightRed,
        ShowBinaryHighlightGreen,
        ShowBinaryHighlightBlue,
        ShowBinaryOnly
    }
    public partial class BinaryProp : UserControl
    {
        public event EventHandler<RangeChangedEventArgs> RangeChanged;

        BlobAlgorithm _blobAlgo = null;

        public int LeftValue => binRangeTrackbar.ValueLeft;
        public int RightValue => binRangeTrackbar.ValueRight;

        private bool _updateDataGridView = true;
        private readonly int COL_USE = 1;
        private readonly int COL_MIN = 2;
        private readonly int COL_MAX = 3;
        public BinaryProp()
        {
            InitializeComponent();

            cbBinMethod.DataSource = Enum.GetValues(typeof(BinaryMethod)).Cast<BinaryMethod>().ToList();
            cbBinMethod.SelectedIndex = (int)BinaryMethod.Feature;

            InitializeFilterDataGridView();

            binRangeTrackbar.RangeChanged += Range_RangeChanged;

            binRangeTrackbar.ValueLeft = 0;
            binRangeTrackbar.ValueRight = 128;

            cmbHighlight.Items.Add("사용안함");
            cmbHighlight.Items.Add("빨간색");
            cmbHighlight.Items.Add("녹색");
            cmbHighlight.Items.Add("파란색");
            cmbHighlight.Items.Add("흑백");
            cmbHighlight.SelectedIndex = 0;
        }

        private void InitializeFilterDataGridView()
        {
            // 컬럼 설정
            dataGridViewFilter.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "필터명",
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 70
            });

            dataGridViewFilter.Columns.Add(new DataGridViewCheckBoxColumn()
            {
                HeaderText = "사용",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 40
            });

            dataGridViewFilter.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "최소값",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 65
            });

            dataGridViewFilter.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "최대값",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 65
            });

            AddFilterRow("Area");
            AddFilterRow("Length");
            AddFilterRow("Width");
            AddFilterRow("Count");

            dataGridViewFilter.AllowUserToAddRows = false;
            dataGridViewFilter.RowHeadersVisible = false;
            dataGridViewFilter.AllowUserToResizeColumns = false;
            dataGridViewFilter.AllowUserToResizeRows = false;
            dataGridViewFilter.AllowUserToOrderColumns = false;
            dataGridViewFilter.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        private void AddFilterRow(string itemName)
        {
            dataGridViewFilter.Rows.Add(itemName, false, "", "");
        }

        public void SetAlgorithm(BlobAlgorithm blobAlgo)
        {
            _blobAlgo = blobAlgo;

            if (_blobAlgo.BlobFilters.Count <= 0)
                blobAlgo.SetDefault();

            SetProperty();
        }

        public void SetProperty()
        {
            if (_blobAlgo is null)
                return;

            chkUse.Checked = _blobAlgo.IsUse;

            BinaryThreshold threshold = _blobAlgo.BinThreshold;

            if (threshold.invert)
            {
                binRangeTrackbar.SetThreshold(threshold.upper, threshold.lower);
            }
            else
            {
                binRangeTrackbar.SetThreshold(threshold.lower, threshold.upper);
            }

            cbBinMethod.SelectedIndex = (int)_blobAlgo.BinMethod;

            UpdateDataGridView(true);
            chkRotatedRect.Checked = _blobAlgo.UseRotatedRect;
        }

        public void GetProperty()
        {
            if (_blobAlgo is null)
                return;

            _blobAlgo.IsUse = chkUse.Checked;

            BinaryThreshold threshold = new BinaryThreshold();

            int leftValue = LeftValue;
            int rightValue = RightValue;

            if (leftValue < rightValue)
            {
                threshold.lower = leftValue;
                threshold.upper = rightValue;
                threshold.invert = false;
            }
            else
            {
                threshold.lower = rightValue;
                threshold.upper = leftValue;
                threshold.invert = true;
            }

            _blobAlgo.BinThreshold = threshold;

            UpdateDataGridView(false);
        }

        private void UpdateDataGridView(bool update)
        {
            if (_blobAlgo is null)
                return;

            if (update)
            {
                _updateDataGridView = false;
                List<BlobFilter> blobFilters = _blobAlgo.BlobFilters;

                for (int i = 0; i < blobFilters.Count; i++)
                {
                    if (i >= dataGridViewFilter.Rows.Count)
                        break;

                    dataGridViewFilter.Rows[i].Cells[COL_USE].Value = blobFilters[i].isUse;
                    dataGridViewFilter.Rows[i].Cells[COL_MIN].Value = blobFilters[i].min;
                    dataGridViewFilter.Rows[i].Cells[COL_MAX].Value = blobFilters[i].max;
                }
                _updateDataGridView = true;
            }
            else
            {
                if (_updateDataGridView == false)
                    return;

                List<BlobFilter> blobFilters = _blobAlgo.BlobFilters;

                for (int i = 0; i < blobFilters.Count; i++)
                {
                    BlobFilter blobFilter = blobFilters[i];
                    blobFilter.isUse = (bool)dataGridViewFilter.Rows[i].Cells[COL_USE].Value;

                    object value = dataGridViewFilter.Rows[i].Cells[COL_MIN].Value;

                    int min = 0;
                    if (value != null && int.TryParse(value.ToString(), out min))
                        blobFilter.min = min;

                    value = dataGridViewFilter.Rows[i].Cells[COL_MAX].Value;

                    int max = 0;
                    if (value != null && int.TryParse(value.ToString(), out max))
                        blobFilter.max = max;
                }
            }
        }


        private void UpdateBinary()
        {
            GetProperty();

            int leftValue = LeftValue;
            int rightValue = RightValue;
            bool invert = false;

            if (leftValue > rightValue)
            {
                leftValue = RightValue;
                rightValue = LeftValue;
                invert = true;
            }

            ShowBinaryMode showBinaryMode = (ShowBinaryMode)cmbHighlight.SelectedIndex;
            RangeChanged?.Invoke(this, new RangeChangedEventArgs(leftValue, rightValue, invert, showBinaryMode));
        }

        private void Range_RangeChanged(object sender, EventArgs e)
        {
            UpdateBinary();
        }

        private void chkUse_CheckedChanged(object sender, EventArgs e)
        {
            bool useBinary = chkUse.Checked;
            grpBinary.Enabled = useBinary;

            dataGridViewFilter.Enabled = useBinary;

            GetProperty();
        }

        private void cmbHighlight_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateBinary();
        }

        //#8_INSPECT_BINARY#14 이벤트 발생시 값에 반영
        private void dataGridViewFilter_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_updateDataGridView == true)
                UpdateDataGridView(false);
        }

        private void dataGridViewFilter_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if(dataGridViewFilter.CurrentCell is DataGridViewCheckBoxCell)
            {
                dataGridViewFilter.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void chkRotatedRect_CheckedChanged(object sender, EventArgs e)
        {
            if (_blobAlgo is null)
                return;

            _blobAlgo.UseRotatedRect = chkRotatedRect.Checked;
        }

        private void cbBinMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_blobAlgo is null)
                return;

            _blobAlgo.BinMethod = (BinaryMethod)cbBinMethod.SelectedIndex;
            chkRotatedRect.Enabled = _blobAlgo.BinMethod == BinaryMethod.Feature;

            if (_blobAlgo.BinMethod == BinaryMethod.PixelCount)
            {
                for (int i = 0; i < dataGridViewFilter.Rows.Count; i++)
                {
                    bool useFeature = i == 0 ? true : false; // Area 필터만 사용 가능
                    dataGridViewFilter.Rows[i].Cells[COL_USE].Value = useFeature;
                }
                dataGridViewFilter.Columns[COL_USE].ReadOnly = true;
            }
            else
            {
                dataGridViewFilter.Columns[COL_USE].ReadOnly = false;
            }

            _updateDataGridView = true;
        }

    }

    //이진화 관련 이벤트 발생시, 전달할 값 추가
    public class RangeChangedEventArgs : EventArgs
    {
        public int LowerValue { get; }
        public int UpperValue { get; }
        public bool Invert { get; }
        public ShowBinaryMode ShowBinMode { get; }

        public RangeChangedEventArgs(int lowerValue, int upperValue, bool invert, ShowBinaryMode showBinaryMode)
        {
            LowerValue = lowerValue;
            UpperValue = upperValue;
            Invert = invert;
            ShowBinMode = showBinaryMode;
        }
    }
}

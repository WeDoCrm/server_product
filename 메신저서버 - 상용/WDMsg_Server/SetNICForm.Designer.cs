namespace WDMsgServer
{
    partial class SetNICForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_comfirm = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbt_type_cid = new System.Windows.Forms.RadioButton();
            this.rbt_type_ss = new System.Windows.Forms.RadioButton();
            this.rbt_type_lg = new System.Windows.Forms.RadioButton();
            this.rbt_type_sip = new System.Windows.Forms.RadioButton();
            this.label_COM_CODE = new System.Windows.Forms.Label();
            this.tbx_com_code = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_comfirm
            // 
            this.btn_comfirm.Location = new System.Drawing.Point(83, 159);
            this.btn_comfirm.Name = "btn_comfirm";
            this.btn_comfirm.Size = new System.Drawing.Size(75, 23);
            this.btn_comfirm.TabIndex = 0;
            this.btn_comfirm.Text = "확인";
            this.btn_comfirm.UseVisualStyleBackColor = true;
            // 
            // btn_cancel
            // 
            this.btn_cancel.Location = new System.Drawing.Point(190, 159);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(75, 23);
            this.btn_cancel.TabIndex = 1;
            this.btn_cancel.Text = "취소";
            this.btn_cancel.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            ":::::::::::::::::선 택::::::::::::::::::::"});
            this.comboBox1.Location = new System.Drawing.Point(75, 123);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(268, 20);
            this.comboBox1.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 127);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "통신장치";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbt_type_cid);
            this.groupBox1.Controls.Add(this.rbt_type_lg);
            this.groupBox1.Controls.Add(this.rbt_type_sip);
            this.groupBox1.Location = new System.Drawing.Point(20, 52);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(324, 50);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "교환기 타입";
            // 
            // rbt_type_cid
            // 
            this.rbt_type_cid.AutoSize = true;
            this.rbt_type_cid.Location = new System.Drawing.Point(241, 23);
            this.rbt_type_cid.Name = "rbt_type_cid";
            this.rbt_type_cid.Size = new System.Drawing.Size(43, 16);
            this.rbt_type_cid.TabIndex = 3;
            this.rbt_type_cid.TabStop = true;
            this.rbt_type_cid.Tag = "CID";
            this.rbt_type_cid.Text = "CID";
            this.rbt_type_cid.UseVisualStyleBackColor = true;
            // 
            // rbt_type_ss
            // 
            this.rbt_type_ss.AutoSize = true;
            this.rbt_type_ss.Location = new System.Drawing.Point(261, 20);
            this.rbt_type_ss.Name = "rbt_type_ss";
            this.rbt_type_ss.Size = new System.Drawing.Size(71, 16);
            this.rbt_type_ss.TabIndex = 2;
            this.rbt_type_ss.TabStop = true;
            this.rbt_type_ss.Tag = "SS";
            this.rbt_type_ss.Text = "삼성키폰";
            this.rbt_type_ss.UseVisualStyleBackColor = true;
            this.rbt_type_ss.Visible = false;
            // 
            // rbt_type_lg
            // 
            this.rbt_type_lg.AutoSize = true;
            this.rbt_type_lg.Location = new System.Drawing.Point(143, 23);
            this.rbt_type_lg.Name = "rbt_type_lg";
            this.rbt_type_lg.Size = new System.Drawing.Size(63, 16);
            this.rbt_type_lg.TabIndex = 1;
            this.rbt_type_lg.TabStop = true;
            this.rbt_type_lg.Tag = "LG";
            this.rbt_type_lg.Text = "LG키폰";
            this.rbt_type_lg.UseVisualStyleBackColor = true;
            // 
            // rbt_type_sip
            // 
            this.rbt_type_sip.AutoSize = true;
            this.rbt_type_sip.Location = new System.Drawing.Point(24, 23);
            this.rbt_type_sip.Name = "rbt_type_sip";
            this.rbt_type_sip.Size = new System.Drawing.Size(83, 16);
            this.rbt_type_sip.TabIndex = 0;
            this.rbt_type_sip.TabStop = true;
            this.rbt_type_sip.Tag = "SIP";
            this.rbt_type_sip.Text = "인터넷전화";
            this.rbt_type_sip.UseVisualStyleBackColor = true;
            // 
            // label_COM_CODE
            // 
            this.label_COM_CODE.AutoSize = true;
            this.label_COM_CODE.Location = new System.Drawing.Point(28, 20);
            this.label_COM_CODE.Name = "label_COM_CODE";
            this.label_COM_CODE.Size = new System.Drawing.Size(53, 12);
            this.label_COM_CODE.TabIndex = 5;
            this.label_COM_CODE.Text = "회사코드";
            // 
            // tbx_com_code
            // 
            this.tbx_com_code.Location = new System.Drawing.Point(87, 14);
            this.tbx_com_code.Name = "tbx_com_code";
            this.tbx_com_code.Size = new System.Drawing.Size(100, 21);
            this.tbx_com_code.TabIndex = 0;
            // 
            // SetNICForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(357, 193);
            this.Controls.Add(this.tbx_com_code);
            this.Controls.Add(this.rbt_type_ss);
            this.Controls.Add(this.label_COM_CODE);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.btn_cancel);
            this.Controls.Add(this.btn_comfirm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "SetNICForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "설정";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button btn_comfirm;
        public System.Windows.Forms.Button btn_cancel;
        public System.Windows.Forms.ComboBox comboBox1;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.RadioButton rbt_type_lg;
        public System.Windows.Forms.RadioButton rbt_type_sip;
        public System.Windows.Forms.RadioButton rbt_type_cid;
        public System.Windows.Forms.RadioButton rbt_type_ss;
        public System.Windows.Forms.Label label_COM_CODE;
        public System.Windows.Forms.TextBox tbx_com_code;
    }
}
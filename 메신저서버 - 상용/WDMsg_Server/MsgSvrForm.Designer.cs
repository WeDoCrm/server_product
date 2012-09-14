namespace WDMsgServer
{
    partial class MsgSvrForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마십시오.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MsgSvrForm));
            this.start = new System.Windows.Forms.Button();
            this.stop = new System.Windows.Forms.Button();
            this.LogBox = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MnControl = new System.Windows.Forms.ToolStripMenuItem();
            this.MnServerStart = new System.Windows.Forms.ToolStripMenuItem();
            this.MnServerStop = new System.Windows.Forms.ToolStripMenuItem();
            this.MnAddMember = new System.Windows.Forms.ToolStripMenuItem();
            this.MnAddTeam = new System.Windows.Forms.ToolStripMenuItem();
            this.MnServerFormClose = new System.Windows.Forms.ToolStripMenuItem();
            this.MnView = new System.Windows.Forms.ToolStripMenuItem();
            this.MnDBSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.button1 = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // start
            // 
            this.start.Location = new System.Drawing.Point(188, 49);
            this.start.Name = "start";
            this.start.Size = new System.Drawing.Size(75, 27);
            this.start.TabIndex = 0;
            this.start.Text = "서버 Start";
            this.start.UseVisualStyleBackColor = true;
            this.start.Click += new System.EventHandler(this.start_Click);
            // 
            // stop
            // 
            this.stop.Location = new System.Drawing.Point(341, 49);
            this.stop.Name = "stop";
            this.stop.Size = new System.Drawing.Size(75, 27);
            this.stop.TabIndex = 1;
            this.stop.Text = "서버 Stop";
            this.stop.UseVisualStyleBackColor = true;
            this.stop.Click += new System.EventHandler(this.stop_Click);
            // 
            // LogBox
            // 
            this.LogBox.BackColor = System.Drawing.Color.Black;
            this.LogBox.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogBox.ForeColor = System.Drawing.Color.White;
            this.LogBox.Location = new System.Drawing.Point(12, 124);
            this.LogBox.Multiline = true;
            this.LogBox.Name = "LogBox";
            this.LogBox.ReadOnly = true;
            this.LogBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LogBox.Size = new System.Drawing.Size(620, 435);
            this.LogBox.TabIndex = 6;
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.Control;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MnControl,
            this.MnView});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(638, 24);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // MnControl
            // 
            this.MnControl.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MnServerStart,
            this.MnServerStop,
            this.MnAddMember,
            this.MnAddTeam,
            this.MnServerFormClose});
            this.MnControl.Name = "MnControl";
            this.MnControl.Size = new System.Drawing.Size(59, 20);
            this.MnControl.Text = "제어(&C)";
            // 
            // MnServerStart
            // 
            this.MnServerStart.Name = "MnServerStart";
            this.MnServerStart.Size = new System.Drawing.Size(154, 22);
            this.MnServerStart.Text = "서버 시작(&S)";
            // 
            // MnServerStop
            // 
            this.MnServerStop.Name = "MnServerStop";
            this.MnServerStop.Size = new System.Drawing.Size(154, 22);
            this.MnServerStop.Text = "서버 중지(&O)";
            // 
            // MnAddMember
            // 
            this.MnAddMember.Enabled = false;
            this.MnAddMember.Name = "MnAddMember";
            this.MnAddMember.Size = new System.Drawing.Size(154, 22);
            this.MnAddMember.Text = "사용자 추가(&A)";
            // 
            // MnAddTeam
            // 
            this.MnAddTeam.Enabled = false;
            this.MnAddTeam.Name = "MnAddTeam";
            this.MnAddTeam.Size = new System.Drawing.Size(154, 22);
            this.MnAddTeam.Text = "소속 추가(&T)";
            // 
            // MnServerFormClose
            // 
            this.MnServerFormClose.Name = "MnServerFormClose";
            this.MnServerFormClose.Size = new System.Drawing.Size(154, 22);
            this.MnServerFormClose.Text = "닫기(&C)";
            // 
            // MnView
            // 
            this.MnView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MnDBSetting});
            this.MnView.Name = "MnView";
            this.MnView.Size = new System.Drawing.Size(59, 20);
            this.MnView.Text = "보기(&V)";
            // 
            // MnDBSetting
            // 
            this.MnDBSetting.Name = "MnDBSetting";
            this.MnDBSetting.Size = new System.Drawing.Size(135, 22);
            this.MnDBSetting.Text = "DB 설정(&O)";
            this.MnDBSetting.Click += new System.EventHandler(this.MnDBSetting_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(542, 95);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 9;
            this.button1.Text = "콜테스트";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // MsgSvrForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(638, 568);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.LogBox);
            this.Controls.Add(this.stop);
            this.Controls.Add(this.start);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MsgSvrForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WeDo 메신저 서버";
            this.Load += new System.EventHandler(this.MsgSvrForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MsgSvrForm_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button start;
        private System.Windows.Forms.Button stop;
        private System.Windows.Forms.TextBox LogBox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem MnControl;
        private System.Windows.Forms.ToolStripMenuItem MnServerStart;
        private System.Windows.Forms.ToolStripMenuItem MnServerStop;
        private System.Windows.Forms.ToolStripMenuItem MnView;
        private System.Windows.Forms.ToolStripMenuItem MnServerFormClose;
        private System.Windows.Forms.ToolStripMenuItem MnAddMember;
        private System.Windows.Forms.ToolStripMenuItem MnAddTeam;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ToolStripMenuItem MnDBSetting;
    }
}


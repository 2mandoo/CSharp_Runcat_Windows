using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RunCatForWin
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        /// 
        private static NotifyIcon notifyIcon; // 시스템 트레이 아이콘 만들기 위한 NotifyIcon 객체
        private static Timer monitoring_timer;
        private static Timer run_timer;

        // 다크모드 고양이
        private static Icon[] darkIconObjects = {
            Properties.Resources.dark_cat_0,
            Properties.Resources.dark_cat_1,
            Properties.Resources.dark_cat_2,
            Properties.Resources.dark_cat_3,
            Properties.Resources.dark_cat_4
        };

        // 라이트모드 고양이
        private static Icon[] lightIconObjects = {
            Properties.Resources.light_cat_0,
            Properties.Resources.light_cat_1,
            Properties.Resources.light_cat_2,
            Properties.Resources.light_cat_3,
            Properties.Resources.light_cat_4
        };

        // 현재 모드 아이콘 객체 배열
        private static Icon[] currentIcons;
        // 현재 아이콘 인덱스
        private static int currentIconIndex = 0;
        // 기본 다크모드
        private static bool isDarkMode = true;

        // PerformanceCounter 객체 (CPU 및 메모리 사용량을 가져오기 위해 사용)
        // Windows 성능 카운터를 사용하여 시스템 성능 데이터를 수집
        private static PerformanceCounter cpuCounter;
        private static PerformanceCounter memoryCounter;

        private static ToolStripMenuItem darkMenuItem;
        private static ToolStripMenuItem lightMenuItem;


        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 다크모드로 기본 세팅
            currentIcons = darkIconObjects;

            /*
             * Processor Information\% Processor Utility | Processor\% Processor Time
             */

            // PerformanceCounter 객체 초기화
            // 전체 CPU가 얼마나 효율적으로 사용되고 있는지를 측정하며, 프로세서의 실제 사용률 나타냄
            // Get-Counter -Counter "\Processor Information(_Total)\% Processor Utility"
            cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            memoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");


            // NotifyIcon 설정
            notifyIcon = new NotifyIcon
            {
                Icon = currentIcons[currentIconIndex], // 초기 아이콘 설정
                Visible = true,
                Text = "CPU Monitoring App"
            };

            // ContextMenuStrip 생성
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            // 테마 메뉴
            var themeMenuItem = new ToolStripMenuItem("Theme");

            // 다크모드 메뉴 항목
            darkMenuItem = new ToolStripMenuItem("Dark", null, (sender, e) => SetTheme(true))
            {
                Checked = isDarkMode // 현재 모드에 따라 초기 체크 상태 설정
            };

            // 라이트모드 메뉴 항목
            lightMenuItem = new ToolStripMenuItem("Light", null, (sender, e) => SetTheme(false))
            {
                Checked = !isDarkMode // 현재 모드에 따라 초기 체크 상태 설정
            };

            // 테마 메뉴에 항목 추가
            themeMenuItem.DropDownItems.Add(darkMenuItem);
            themeMenuItem.DropDownItems.Add(lightMenuItem);
            contextMenu.Items.Add(themeMenuItem);

            // Exit
            contextMenu.Items.Add("Exit", null, (sender, e) => ExitApplication(notifyIcon));

            notifyIcon.ContextMenuStrip = contextMenu;

            // Timer 설정
            monitoring_timer = new Timer();
            monitoring_timer.Interval = 1000;
            monitoring_timer.Tick += Monitor_Timer_Tick;
            monitoring_timer.Start();

            run_timer = new Timer();
            run_timer.Interval = 500;
            run_timer.Tick += Run_Timer_Tick;
            run_timer.Start();

            // 메시지 루프 시작 (폼 없음)
            Application.Run();
        }

        static void SetTheme(bool darkMode)
        {
            isDarkMode = darkMode;

            darkMenuItem.Checked = darkMode;
            lightMenuItem.Checked = !darkMode;

            // 테마 바꾸면서 인덱스 초기화
            currentIconIndex = 0;

            currentIcons = isDarkMode ? darkIconObjects : lightIconObjects;
            notifyIcon.Icon = currentIcons[currentIconIndex];
        }

        static void Monitor_Timer_Tick(object sender, EventArgs e)
        {
            // CPU 및 메모리 사용량
            float cpuUsage = GetCpuUsage();
            float memoryUsage = GetMemoryUsage();

            UpdateTimerInterval(cpuUsage); // 아이콘 변경 속도 조정

            // 툴팁 텍스트 업데이트
            notifyIcon.Text = $"CPU Usage: {cpuUsage:F1}% | Memory Usage: {memoryUsage:F1}%";
        }

        static void Run_Timer_Tick(object sender, EventArgs e)
        {
            // 현재 아이콘 인덱스를 변경하고 아이콘을 업데이트
            currentIconIndex = (currentIconIndex + 1) % currentIcons.Length;
            notifyIcon.Icon = currentIcons[currentIconIndex];
        }

        static void UpdateTimerInterval(float cpuUsage)
        {
            // CPU 사용량에 고양이 속도 빠르게
            if (cpuUsage > 30)
            {
                run_timer.Interval = 100;
            }
            else if (cpuUsage > 20)
            {
                run_timer.Interval = 250;
            }
            else
            {
                run_timer.Interval = 500;
            }
        }

        static float GetCpuUsage()
        {
            return cpuCounter.NextValue();
        }

        static float GetMemoryUsage()
        {
            return memoryCounter.NextValue();
        }

        static void ExitApplication(NotifyIcon notifyIcon)
        {
            notifyIcon.Visible = false;
            //notifyIcon.Dispose();  // 시스템 트레이 아이콘 리소스 해제

            // 리소스 해제
            cpuCounter.Close();
            memoryCounter.Close();

            Application.Exit();
        }

    }
}

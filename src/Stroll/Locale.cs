using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Stroll
{
    public static class Locale
    {
        public const string English = "en";
        public const string CantoneseHongKong = "zh-HK";
        private static readonly Dictionary<string, string> Cantonese = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Timing", "時間" }, { "Break screen", "休息畫面" }, { "Pets on screen", "畫面上的寵物" }, { "Pet && path", "寵物及路線" },
            { "Save settings", "儲存設定" }, { "Preview break", "預覽休息畫面" }, { "Close", "關閉" },
            { "A quieter rhythm. Work, take a proper break, then start fresh.", "調整工作與休息節奏，休息過後重新開始。" },
            { "Work interval · minutes", "工作時間 · 分鐘" }, { "Break duration · minutes", "休息時間 · 分鐘" }, { "Snooze delay · minutes", "延後提示 · 分鐘" },
            { "Show a warning before the prompt", "休息提示前顯示提醒" }, { "Advance warning", "預先提醒" }, { "Warning lead · seconds", "提醒時間 · 秒" },
            { "Start quietly in the tray when I sign in", "登入 Windows 時在系統匣中啟動" }, { "Windows startup", "Windows 啟動" },
            { "Start / taskbar pinning", "開始功能表／工作列" }, { "Add to Start menu", "加入開始功能表" },
            { "In Start, search for Stroll and right-click to pin to Start or taskbar.\nA pinned launch opens settings even when Stroll is already in the tray.", "在開始功能表搜尋 Stroll，按右鍵即可釘選到開始功能表或工作列。\n即使 Stroll 已在系統匣執行，開啟釘選捷徑仍可顯示設定。" },
            { "Snooze asks again later. Skip starts a fresh work interval.\nSleep/wake starts fresh. Pause is manual; there is no activity monitoring.", "延後會稍後再次提醒；略過會開始新的工作時間。\n電腦睡眠後喚醒會重新計時。暫停由你手動控制，程式不會監察活動。" },
            { "Gently fade in and out", "淡入及淡出" }, { "Transitions", "轉場效果" }, { "Fade in / out duration", "淡入／淡出時間" },
            { "Break reminder", "休息提示文字" }, { "Choose colour…", "選擇顏色…" }, { "Background colour", "背景顏色" },
            { "Background image", "背景圖片" }, { "Upload image…", "匯入圖片…" }, { "Use solid colour", "使用純色" }, { "Set to default", "還原預設顏色" },
            { "Selected image", "目前圖片" }, { "Solid colour", "純色" },
            { "S-Yosef's bundled jump pose", "S-Yosef 的內置跳躍姿勢" }, { "None · use main image during hop", "無 · 跳躍時使用主要圖片" }, { "Bundled jump pose", "內置跳躍姿勢" },
            { "Colour wheel, brightness and hex entry are supported.\nImages fill the screen and are cropped to fit. The reminder stays readable.", "支援色環、亮度調整及十六進位色碼。\n圖片會裁切以填滿畫面，提示文字仍會保持清晰。" },
            { "All monitors are covered during the break. Your pet and controls appear\non the primary display; other displays show the reminder and countdown.", "休息時會覆蓋所有螢幕。寵物和控制按鈕會顯示在主螢幕，\n其他螢幕則顯示提示文字和倒數時間。" },
            { "Esc snoozes a live break. The overlay can also be closed with Alt+F4.\nNo keyboard hooks, screen capture, accounts, uploads to a server or telemetry.", "休息畫面中按 Esc 會延後休息；亦可按 Alt+F4 關閉。\n程式不會攔截鍵盤、擷取畫面、建立帳戶、上傳資料或收集使用資訊。" },
            { "Pet presets", "寵物預設" }, { "Edit selected", "編輯所選項目" }, { "Save named pet", "儲存寵物預設" }, { "Delete pet", "刪除寵物" }, { "Pet name", "寵物名稱" },
            { "Walking image / GIF", "行走圖片／GIF" }, { "Upload pet…", "匯入寵物…" }, { "Selected pet", "目前寵物" }, { "Jump image / GIF · optional", "跳躍圖片／GIF · 可選" },
            { "Upload jump…", "匯入跳躍圖片…" }, { "Clear jump", "清除跳躍圖片" }, { "Selected jump", "目前跳躍圖片" },
            { "Bottom", "下方" }, { "Right", "右方" }, { "Top", "上方" }, { "Left", "左方" }, { "Walk along", "移動路線" },
            { "Pet size · pixels", "寵物大小 · 像素" }, { "Walking speed · pixels/sec", "行走速度 · 像素／秒" },
            { "Pet faces right in your source image. Transparent PNGs and GIFs work best.\nOpposite edges trigger a visible hop; a jump file is optional. Try Preview break.", "圖片中的寵物請面向右方；透明背景 PNG 或 GIF 效果最佳。\n選擇相對的螢幕邊緣會觸發跳躍；跳躍圖片可選。你可以先預覽。" },
            { "Total pets", "寵物總數" }, { "Show these pets", "顯示以下寵物" }, { "Manage pets", "管理寵物" }, { "Delete selected", "刪除所選項目" }, { "Restore built-ins", "還原內置寵物" },
            { "Check the pets to show together. Extra copies repeat the checked pets.\nUse Pet & path to edit artwork and save more named pets.", "勾選要同時顯示的寵物；增加數量時會重複顯示已勾選的寵物。\n前往「寵物及路線」可編輯圖片和儲存其他寵物。" },
            { "Keep at least one pet in your collection.", "請至少保留一隻寵物。" }, { "Choose a background", "選擇背景圖片" }, { "Choose pet artwork", "選擇寵物圖片" },
            { "That image could not be imported. ", "無法匯入該圖片。" }, { "Import image", "匯入圖片" }, { "Give your pet a name first.", "請先輸入寵物名稱。" },
            { "Choose a file smaller than 20 MB.", "請選擇小於 20 MB 的檔案。" },
            { "Choose an image no larger than 24 megapixels and 8192 pixels per side.", "圖片不可超過 2,400 萬像素或每邊 8,192 像素。" },
            { "Pet images must be 2048 pixels per side or smaller.", "寵物圖片每邊不可超過 2,048 像素。" },
            { "This animation is too large. Reduce its resolution or number of frames.", "動畫檔太大，請降低解像度或減少影格數。" },
            { "Choose at least one edge for your pet.", "請至少選擇一條寵物移動路線。" },
            { "Check at least one pet to show on screen.", "請至少勾選一隻要顯示的寵物。" }, { "Could not save settings. ", "無法儲存設定。" },
            { "Could not add the shortcut. ", "無法加入捷徑。" }, { "Start menu", "開始功能表" }, { "Pet path", "寵物路線" },
            { "Stroll · A little space for your eyes", "Stroll · 讓眼睛稍作休息" }, { "Images", "圖片" },
            { "Choose a background colour", "選擇背景顏色" }, { "Brightness", "亮度" }, { "Use colour", "使用此顏色" }, { "Enter a colour such as #172B29.", "請輸入顏色代碼，例如 #F2EAAA。" }, { "Choose a colour", "選擇顏色" },
            { "Look far. Relax your eyes.", "望遠一點，讓眼睛休息。" }, { "Let your gaze rest somewhere beyond the screen.", "將視線移到螢幕以外，讓眼睛休息。" },
            { "PREVIEW · Esc to close", "預覽 · 按 Esc 關閉" }, { "Close preview", "關閉預覽" }, { "Skip", "略過" }, { "Break", "開始休息" },
            { "A little distance. A little rest.", "望遠一點，休息片刻。" }, { "Time for a break · waiting for you", "休息時間到了 · 等候你的選擇" },
            { "Take a break now", "立即休息" }, { "Pause", "暫停" }, { "Resume", "繼續" }, { "Restart work interval", "重新開始工作時間" }, { "Settings…", "設定…" }, { "Exit", "結束" },
            { "A little rest is coming", "休息時間快到了" }, { "Your break prompt will appear in ", "休息提示將於 " }, { " seconds.", " 秒後顯示。" },
            { "Ready when you are", "準備好便可開始" }, { "Your next work interval has started.", "新一輪工作時間已開始。" },
            { "Stroll · Time for a break", "Stroll · 休息時間" }, { "Ready for a ", "準備好休息 " }, { "-minute eye break?\n", " 分鐘，讓眼睛休息一下嗎？\n" }, { " will keep you company.", " 會陪伴你。" }
        };

        public static bool IsCantonese(string language) { return language == CantoneseHongKong; }
        public static string LanguageButton(string language) { return IsCantonese(language) ? "🌐 廣東話" : "🌐 English"; }
        public static string T(string text, string language)
        {
            if (!IsCantonese(language) || text == null) return text;
            string value;
            if (Cantonese.TryGetValue(text, out value)) return value;
            if (text.StartsWith("Snooze ", StringComparison.Ordinal)) return "延後 " + text.Substring(7).Replace(" min", " 分鐘");
            if (text.EndsWith(" seconds each way", StringComparison.Ordinal)) return text.Substring(0, text.Length - 18) + " 秒（每次）";
            if (text.StartsWith("Resting · ", StringComparison.Ordinal)) return "休息中 · 剩餘 " + text.Substring(10).Replace(" remaining", "");
            if (text.StartsWith("Snoozed · asking again in ", StringComparison.Ordinal)) return "已延後 · " + text.Substring(27) + " 後再次提醒";
            if (text.StartsWith("Paused · ", StringComparison.Ordinal)) return "已暫停 · 從系統匣繼續";
            if (text.StartsWith("Next break in ", StringComparison.Ordinal)) return "距離下次休息還有 " + text.Substring(14);
            if (text.StartsWith("Stroll · ", StringComparison.Ordinal)) return "Stroll · " + T(text.Substring(9), language);
            return text;
        }
        public static string BreakMessage(string message, string language)
        {
            if (message == "Look far. Relax your eyes." || message == "望遠一點，讓眼睛休息。")
                return IsCantonese(language) ? "望遠一點，讓眼睛休息。" : "Look far. Relax your eyes.";
            return message;
        }
        public static void Apply(Control control, string language)
        {
            if (control == null) return;
            control.Text = T(control.Text, language);
            foreach (Control child in control.Controls) Apply(child, language);
            var tabs = control as TabControl;
            if (tabs != null) foreach (TabPage page in tabs.TabPages) Apply(page, language);
        }
        public static void Apply(ToolStripItemCollection items, string language)
        { foreach (ToolStripItem item in items) item.Text = T(item.Text, language); }
    }
}

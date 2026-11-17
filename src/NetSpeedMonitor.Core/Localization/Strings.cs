namespace NetSpeedMonitor.Core.Localization;

/// <summary>Translation table. Column order: English, Arabic, French.</summary>
public static class Strings
{
    public static string Get(string key, int lang)
    {
        if (!Table.TryGetValue(key, out var v)) return key;
        string s = lang < v.Length ? v[lang] : "";
        return string.IsNullOrEmpty(s) ? v[0] : s;
    }

    public static IEnumerable<string> Keys => Table.Keys;

    private static readonly Dictionary<string, string[]> Table = new()
    {
        ["app.name"] = new[] { "NetSpeed Monitor", "مراقب سرعة الإنترنت", "Moniteur de vitesse réseau" },

        // tray / widget menu
        ["menu.toggle"] = new[] { "Show/Hide Speed Monitor", "إظهار/إخفاء عداد السرعة", "Afficher/Masquer le moniteur" },
        ["menu.stats"] = new[] { "Network Statistics", "إحصائيات الشبكة", "Statistiques réseau" },
        ["menu.settings"] = new[] { "Settings", "الإعدادات", "Paramètres" },
        ["menu.reset"] = new[] { "Reset Statistics", "إعادة تعيين الإحصائيات", "Réinitialiser les statistiques" },
        ["menu.startup"] = new[] { "Start with Windows", "التشغيل مع ويندوز", "Démarrer avec Windows" },
        ["menu.about"] = new[] { "About", "حول البرنامج", "À propos" },
        ["menu.exit"] = new[] { "Exit", "خروج", "Quitter" },
        ["menu.resetpos"] = new[] { "Reset widget position", "إعادة موضع العداد", "Réinitialiser la position" },

        // dashboard
        ["dash.title"] = new[] { "Network Statistics", "إحصائيات الشبكة", "Statistiques réseau" },
        ["tab.live"] = new[] { "Live", "مباشر", "En direct" },
        ["tab.usage"] = new[] { "Usage History", "سجل الاستهلاك", "Historique" },
        ["tab.adapters"] = new[] { "Adapters", "محولات الشبكة", "Adaptateurs" },
        ["live.down"] = new[] { "Download", "التنزيل", "Réception" },
        ["live.up"] = new[] { "Upload", "الرفع", "Envoi" },
        ["live.totalDown"] = new[] { "Downloaded (this session)", "تم تنزيله (الجلسة الحالية)", "Reçu (cette session)" },
        ["live.totalUp"] = new[] { "Uploaded (this session)", "تم رفعه (الجلسة الحالية)", "Envoyé (cette session)" },
        ["live.adapter"] = new[] { "Active adapter", "المحول النشط", "Adaptateur actif" },
        ["live.status"] = new[] { "Connection status", "حالة الاتصال", "État de la connexion" },
        ["status.connected"] = new[] { "Connected", "متصل", "Connecté" },
        ["status.disconnected"] = new[] { "Disconnected", "غير متصل", "Déconnecté" },
        ["graph.title"] = new[] { "Real-time traffic (last 2 minutes)", "حركة البيانات الآن (آخر دقيقتين)", "Trafic en temps réel (2 dernières minutes)" },
        ["usage.today"] = new[] { "Today", "اليوم", "Aujourd'hui" },
        ["usage.week"] = new[] { "This week", "هذا الأسبوع", "Cette semaine" },
        ["usage.month"] = new[] { "This month", "هذا الشهر", "Ce mois-ci" },
        ["usage.total"] = new[] { "Total", "الإجمالي", "Total" },
        ["usage.daily"] = new[] { "Daily usage (last 30 days)", "الاستهلاك اليومي (آخر 30 يوم)", "Utilisation quotidienne (30 derniers jours)" },
        ["usage.date"] = new[] { "Date", "التاريخ", "Date" },
        ["usage.sum"] = new[] { "Sum", "المجموع", "Somme" },
        ["adapters.auto"] = new[] { "Automatic (recommended)", "تلقائي (موصى به)", "Automatique (recommandé)" },
        ["adapters.autoHint"] = new[] { "Counts only real adapters, so VPN and virtual traffic is never counted twice.", "يحسب الكروت الفعلية فقط، فلا يتم عد حركة الـ VPN أو الكروت الافتراضية مرتين.", "Ne compte que les adaptateurs réels : le trafic VPN/virtuel n'est jamais compté deux fois." },
        ["adapters.up"] = new[] { "Up", "يعمل", "Actif" },
        ["adapters.down"] = new[] { "Down", "متوقف", "Inactif" },
        ["adapters.vpn"] = new[] { "VPN", "VPN", "VPN" },
        ["adapters.virtual"] = new[] { "virtual", "افتراضي", "virtuel" },

        // settings
        ["set.title"] = new[] { "Settings", "الإعدادات", "Paramètres" },
        ["set.general"] = new[] { "General", "عام", "Général" },
        ["set.appearance"] = new[] { "Appearance", "المظهر", "Apparence" },
        ["set.monitoring"] = new[] { "Monitoring", "المراقبة", "Surveillance" },
        ["set.position"] = new[] { "Widget position", "موضع العداد", "Position du widget" },
        ["set.language"] = new[] { "Language", "اللغة", "Langue" },
        ["lang.system"] = new[] { "System default", "لغة النظام", "Langue du système" },
        ["lang.en"] = new[] { "English", "English", "English" },
        ["lang.ar"] = new[] { "العربية", "العربية", "العربية" },
        ["lang.fr"] = new[] { "Français", "Français", "Français" },
        ["set.startWithWindows"] = new[] { "Start automatically with Windows", "التشغيل تلقائياً مع ويندوز", "Démarrer automatiquement avec Windows" },
        ["set.startMinimized"] = new[] { "Start minimized to tray (don't open the dashboard)", "البدء مصغراً بجوار الساعة (دون فتح لوحة الإحصائيات)", "Démarrer réduit (sans ouvrir le tableau de bord)" },
        ["set.minimizeToTray"] = new[] { "Minimize to tray", "التصغير إلى منطقة الإشعارات", "Réduire dans la zone de notification" },
        ["set.dblClick"] = new[] { "Open dashboard on double-click", "فتح لوحة الإحصائيات بالنقر المزدوج", "Ouvrir le tableau de bord par double-clic" },
        ["set.theme"] = new[] { "Theme", "السمة", "Thème" },
        ["theme.system"] = new[] { "System", "النظام", "Système" },
        ["theme.dark"] = new[] { "Dark", "داكن", "Sombre" },
        ["theme.light"] = new[] { "Light", "فاتح", "Clair" },
        ["set.font"] = new[] { "Font", "الخط", "Police" },
        ["set.fontSize"] = new[] { "Font size", "حجم الخط", "Taille de police" },
        ["set.downColor"] = new[] { "Download color", "لون التنزيل", "Couleur de réception" },
        ["set.upColor"] = new[] { "Upload color", "لون الرفع", "Couleur d'envoi" },
        ["set.bgColor"] = new[] { "Background color", "لون الخلفية", "Couleur d'arrière-plan" },
        ["set.bgDefault"] = new[] { "Theme default", "حسب السمة", "Selon le thème" },
        ["set.transparent"] = new[] { "Transparent background", "خلفية شفافة", "Arrière-plan transparent" },
        ["set.opacity"] = new[] { "Background opacity", "شفافية الخلفية", "Opacité de l'arrière-plan" },
        ["set.layout"] = new[] { "Layout", "التخطيط", "Disposition" },
        ["layout.h"] = new[] { "Horizontal", "أفقي", "Horizontale" },
        ["layout.v"] = new[] { "Vertical", "رأسي", "Verticale" },
        ["set.width"] = new[] { "Widget width (0 = automatic)", "عرض العداد (0 = تلقائي)", "Largeur du widget (0 = auto)" },
        ["set.refresh"] = new[] { "Refresh interval", "معدل التحديث", "Intervalle d'actualisation" },
        ["set.unit"] = new[] { "Speed unit", "وحدة السرعة", "Unité de vitesse" },
        ["unit.bytes"] = new[] { "Bytes (KB/s, MB/s)", "بايت (KB/s, MB/s)", "Octets (Ko/s, Mo/s)" },
        ["unit.bits"] = new[] { "Bits (Kbps, Mbps)", "بت (Kbps, Mbps)", "Bits (Kbps, Mbps)" },
        ["set.precision"] = new[] { "Decimal places", "عدد الخانات العشرية", "Décimales" },
        ["set.showDown"] = new[] { "Show download speed", "إظهار سرعة التنزيل", "Afficher la vitesse de réception" },
        ["set.showUp"] = new[] { "Show upload speed", "إظهار سرعة الرفع", "Afficher la vitesse d'envoi" },
        ["set.adapterAuto"] = new[] { "Choose the adapter automatically", "اختيار المحول تلقائياً", "Choisir l'adaptateur automatiquement" },
        ["set.adapter"] = new[] { "Adapter", "المحول", "Adaptateur" },
        ["set.autoPos"] = new[] { "Align to the taskbar automatically", "المحاذاة تلقائياً مع شريط المهام", "Aligner automatiquement sur la barre des tâches" },
        ["set.lockPos"] = new[] { "Lock position (no dragging)", "قفل الموضع (منع السحب)", "Verrouiller la position" },
        ["set.alwaysTop"] = new[] { "Always on top (manual position)", "دائماً في المقدمة (الموضع اليدوي)", "Toujours au premier plan (position manuelle)" },
        ["set.posX"] = new[] { "Manual X (px)", "الموضع الأفقي X (بكسل)", "X manuel (px)" },
        ["set.posY"] = new[] { "Manual Y (px)", "الموضع الرأسي Y (بكسل)", "Y manuel (px)" },
        ["set.posHint"] = new[] { "Tip: untick automatic alignment, then drag the widget with the mouse.", "ملحوظة: ألغِ المحاذاة التلقائية ثم اسحب العداد بالماوس.", "Astuce : désactivez l'alignement automatique puis faites glisser le widget." },
        ["btn.ok"] = new[] { "OK", "موافق", "OK" },
        ["btn.cancel"] = new[] { "Cancel", "إلغاء", "Annuler" },
        ["btn.apply"] = new[] { "Apply", "تطبيق", "Appliquer" },
        ["btn.choose"] = new[] { "Choose…", "اختيار…", "Choisir…" },
        ["btn.default"] = new[] { "Default", "الافتراضي", "Par défaut" },

        // dialogs
        ["about.text"] = new[]
        {
            "A lightweight real-time network speed monitor for the Windows taskbar.\nNo telemetry. All data stays on this PC.",
            "عداد خفيف لسرعة الإنترنت لحظة بلحظة بجوار ساعة ويندوز.\nبدون أي تتبع. كل البيانات تبقى على جهازك.",
            "Un moniteur de vitesse réseau léger pour la barre des tâches Windows.\nAucune télémétrie. Toutes les données restent sur ce PC."
        },
        ["about.company"] = new[] { "Qaswara Elfohrar Programming for Information Technology", "برمجة قسورة الفوهرر لتقنية المعلومات", "Qaswara Elfohrar Programmation pour les technologies de l'information" },
        ["about.edition"] = new[] { "2026 Edition", "إصدار 2026", "Édition 2026" },
        ["about.website"] = new[] { "Visit our website", "زيارة موقعنا", "Visiter notre site web" },
        ["about.facebook"] = new[] { "Facebook page", "صفحتنا على فيسبوك", "Page Facebook" },
        ["btn.close"] = new[] { "Close", "إغلاق", "Fermer" },
        ["about.version"] = new[] { "Version {0}", "الإصدار {0}", "Version {0}" },
        ["reset.confirm"] = new[] { "Reset all statistics and usage history?", "هل تريد إعادة تعيين كل الإحصائيات وسجل الاستهلاك؟", "Réinitialiser toutes les statistiques et l'historique ?" },
        ["already.running"] = new[] { "NetSpeed Monitor is already running.", "البرنامج يعمل بالفعل.", "NetSpeed Monitor est déjà en cours d'exécution." },
        ["tray.tooltip"] = new[] { "↓ {0}   ↑ {1}", "↓ {0}   ↑ {1}", "↓ {0}   ↑ {1}" },
    };
}

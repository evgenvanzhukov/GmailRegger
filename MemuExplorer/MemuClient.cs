using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace MemuExplorer
{
    public class MemuClient
    {
        public string BaseDir { get; set; }
        public int Index { get; set; }
        public DesiredCapabilities Capa { get; set; }
        public AndroidDriver<IWebElement> Driver { get; set; }
        public WebDriverWait Wait { get; set; }
        string Package { get; set; } 
        string Activity { get; set; }
        public int AppiumId { get; set; }

        public MemuClient(int index, string package = "com.android.settings", string activity = ".Settings")
        {
            Index = index;
            Package = package;
            Activity = activity;
            Capa = new DesiredCapabilities(); // экземпляр класса настроек
            //cap.SetCapability("deviceName", "127.0.0.1:21503"); // имя девайся (абсолютно любое)
            //cap.SetCapability("platformVersion", "4.2.2");// версия платформы (тоже любая)
            Capa.SetCapability("platformName", "Android");//имя платформы
            Capa.SetCapability("appPackage", Package); // пространство имен используемое приложением
            Capa.SetCapability("appActivity", Activity); // активное окно

            Capa.SetCapability(MobileCapabilityType.DeviceName, "MEmu_" + index.ToString());
            Capa.SetCapability("udid", "127.0.0.1:215" + index.ToString() + "3");
        }

        public AndroidDriver<IWebElement> StartDriver(int waitSek = 50, string package = "", string activity = "")
        {
            if (activity!="")
            {
                Capa.SetCapability("appPackage", Package); // пространство имен используемое приложением
                Capa.SetCapability("appActivity", Activity); // активное окно
            }
            int port = (Index > 3) ? 4721 + Index : 4720 + Index;
            Driver = new AndroidDriver<IWebElement>(new Uri("http://127.0.0.1:" + port.ToString() + "/wd/hub"), Capa); // инициализируем
            Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(waitSek));;
            return Driver;
        }

        public void StartMemu()
        {
            string index = (Index == 0) ? "" : "_"+Index.ToString();
            string memu = "MEmu" + index;
            string memuDir = Path.Combine(BaseDir, @"MemuHyperv VMs\" + memu);
            if (!Directory.Exists(memuDir)) throw new DirectoryNotFoundException(memuDir);

            string logDir = Path.Combine(memuDir, "Logs");

            if (!File.Exists(BaseDir + "MEmuConsole.exe")) throw new FileNotFoundException(BaseDir + "MEmuConsole.exe");
            ProcessStartInfo sti = new ProcessStartInfo(BaseDir + "memuconsole.exe", memu);
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(BaseDir + @"\MEmuConsole.exe", memu);
            p.Start();
            //Console.WriteLine("starting MEmu_" + index);
            Thread.Sleep(30000);

            if (!Directory.Exists(logDir)) throw new DirectoryNotFoundException(logDir);

            var logSize = new FileInfo(logDir + @"\MEmu.log").Length;
            var maxTime = 0;
            while (logSize < 67700 && maxTime < 50)
            {
                Thread.Sleep(1000);
                maxTime = maxTime + 1;
            }
            //Console.WriteLine(Process.GetProcessesByName("memu").First(m => m.MainWindowTitle.Contains("MEmu_" + index.ToString())).VirtualMemorySize64.ToString());

            long memory = Process.GetProcessesByName("memu").First(m => m.MainWindowTitle.Contains("MEmu")&& m.MainWindowTitle.Contains(index.Replace("_",""))).VirtualMemorySize64;
            int sek = 0;
            while (sek < 40 && memory < 360000000)
            {
                Thread.Sleep(1000);
                sek += 1;
                memory = Process.GetProcessesByName("memu").First(m => m.MainWindowTitle.Contains("MEmu") && m.MainWindowTitle.Contains(index.Replace("_", ""))).VirtualMemorySize64;
                Console.WriteLine(memory);
            }
            Console.WriteLine("40 sek end + 10");
//            Process.Start(adb).WaitForExit();
            Thread.Sleep(10000);
//            Console.WriteLine("set connect and start appium");
            //Console.ReadLine();
        }

        public void ConnectToAdb()
        {
            string adb = BaseDir+"adb.exe";
            if (!File.Exists(adb)) throw new FileNotFoundException(adb);

            Process adbProcess = new Process();//.GetProcessesByName("adb").First(p=>p.MainModule.FileName.Equals(adb));
            adbProcess.StartInfo = new ProcessStartInfo(adb);
            adbProcess.StartInfo.Arguments = "connect 127.0.0.1:215" + Index.ToString() + "3";

            adbProcess.Start();
            adbProcess.WaitForExit();
        }

        public void InitAppiumServer(bool stopAllServers=false,string dopArgs= "--relaxed-security")
        {
            int index = Index;
            if (stopAllServers) try { Process.GetProcessesByName("node").ToList().ForEach(n=>n.Kill()); } catch { }
            Process startAppium = new Process();
            string appium = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\npm\appium.cmd";
            if (!File.Exists(appium)) throw new FileNotFoundException(appium);
            startAppium.StartInfo = new ProcessStartInfo(appium);
            int port = (index > 3) ? 4721+index : 4720+index;
            startAppium.StartInfo.Arguments = "-a 127.0.0.1 -p " + port.ToString();
            int bp = (index > 3) ? 4731 + index : 4730 + index;
            startAppium.StartInfo.Arguments += " --bootstrap-port " + Convert.ToString(bp);
            if (dopArgs!="") startAppium.StartInfo.Arguments += " "+dopArgs;
            startAppium.Start();
            Thread.Sleep(10000 + Process.GetProcessesByName("memu").Length * 10000);
            AppiumId = startAppium.Id;
        }


        public void DisconnectFromAdb()
        {
            int index = Index;
            string adb = BaseDir + "\adb.exe";

            Process adbProcess = new Process();//.GetProcessesByName("adb").First(p=>p.MainModule.FileName.Equals(adb));
            adbProcess.StartInfo = new ProcessStartInfo(adb);
            adbProcess.StartInfo.Arguments = "disconnect 127.0.0.1:215" + Index.ToString() + "3";

            adbProcess.Start();
            adbProcess.WaitForExit();
        }


        public Tuple<bool, IWebElement> AwaitText(AndroidDriver<IWebElement> driver, string text, int sek = 20)
        {
            var by = By.XPath("//[contains(@text, '" + text + "')][last()]");
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(sek));
            try
            {
                wait.Until(ExpectedConditions.ElementIsVisible(by));
                return new Tuple<bool, IWebElement>(true, driver.FindElement(by));
            }
            catch
            {
                return new Tuple<bool, IWebElement>(false, driver.FindElement(By.XPath("//")));
            }
        }

        public Tuple<bool,IWebElement> AwaitElement(AndroidDriver<IWebElement> driver, string text, int sek=20, string type = "TextView", string param = "text", string nameSpace = "android.widget")
        {
            var by = By.XPath("//" + nameSpace + "." + type + "[contains(@" + param + ", '" + text + "')]");
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(sek));
            try
            {
                wait.Until(ExpectedConditions.ElementExists(by));
                //wait.Until(ExpectedConditions.ElementIsVisible(by));
                return new Tuple<bool, IWebElement>(true,driver.FindElement(by));
            }
            catch
            {
                return new Tuple<bool, IWebElement>(false, new Object() as IWebElement);
            }
        }

        public Tuple<bool, IWebElement> AwaitElementClass(AndroidDriver<IWebElement> driver, int sek=20, string type = "EditText", string nameSpace = "android.widget")
        {
            
            var by = By.ClassName(nameSpace + "." + type);
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(sek));
            try
            {
                wait.Until(ExpectedConditions.ElementExists(by));
                //wait.Until(ExpectedConditions.ElementIsVisible(by));
                return new Tuple<bool, IWebElement>(true, driver.FindElement(by));
            }
            catch
            {
                return new Tuple<bool, IWebElement>(false, driver.FindElement(By.XPath("//")));
            }
        }



        public bool CheckDriver(AndroidDriver<IWebElement> driver)
        {
            try
            {
                string test = driver.CurrentActivity;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void InstallApk(string ApkFileName)
        {
            Driver.InstallApp(ApkFileName);
        }

        public void ChangeMemuParam(string Param, string Value)
        {

        }

        public void SetProxy (string ip, string port, string type = "SOCKS5", string login="",string pass="")
        {
            Driver = StartDriver(30, "org.proxydroid", ".ProxyDroid");
            //Driver.StartActivity("org.proxydroid", ".ProxyDroid");
            //superuser
            var su = AwaitElement(Driver, "Не спрашивать", 20, "RadioButton");
            if (su.Item1)
            {
                su.Item2.Click();
            
            var confBtn = AwaitElement(Driver, "Разрешить", 20, "Button").Item2;
            if (confBtn.Enabled) confBtn.Click();
            else {
                Thread.Sleep(5000);
                confBtn.Click();
            }
            }

            //если кнопка ХОРОШО
            if (AwaitElement(Driver, "Хорошо", 10, "Button").Item1) AwaitElement(Driver, "Хорошо", 20, "Button").Item2.Click();

            //setProxy
            AwaitElement(Driver, "Адрес").Item2.Click();
            AwaitElementClass(Driver).Item2.SendKeys(ip);
            AwaitElement(Driver, "ОК").Item2.Click();

            AwaitElement(Driver, "Порт").Item2.Click();
            var p = AwaitElementClass(Driver).Item2;
            p.Clear();
            p.SendKeys(port);
            AwaitElement(Driver, "ОК").Item2.Click();

            var t = AwaitElement(Driver, "Тип прокси-сервера");
            if (t.Item1) t.Item2.Click();
            else {
                try
                {
                    Driver.Swipe(15, Driver.Manage().Window.Size.Height - 50, 15, 25, 500);
                }
                catch { }
                AwaitElement(Driver, "Тип прокси-сервера").Item2.Click();
            }
            //AwaitElementClass(Driver).Item2.SendKeys(ip);
            AwaitElement(Driver, type,20, "CheckedTextView").Item2.Click();

            if (login !="")
            {
                //set auth
                try {
                    Driver.Swipe(15, Driver.Manage().Window.Size.Height - 50, 15, 25, 500);
                } catch { }
                AwaitElement(Driver,"Включить авторизацию").Item2.Click();

                AwaitElement(Driver, "Пользователь").Item2.Click();
                AwaitElementClass(Driver).Item2.SendKeys(login);
                AwaitElement(Driver, "ОК", 20, "Button").Item2.Click();

                AwaitElement(Driver, "Пароль").Item2.Click();
                AwaitElementClass(Driver).Item2.SendKeys(pass);
                AwaitElement(Driver, "ОК", 20, "Button").Item2.Click();
            }

            //enable proxy
            try
            {
                Driver.Swipe(15, 25, 15, Driver.Manage().Window.Size.Height - 50, 500);
            }
            catch { }

            var on = AwaitElement(Driver, "ВЫКЛ", 20, "Switch") ;
            if (on.Item1) on.Item2.Click();
            else
            {
                try { Driver.Swipe(15, 25, 15, Driver.Manage().Window.Size.Height - 50, 500);
                } catch { }
                AwaitElement(Driver, "ВЫКЛ", 20, "Switch").Item2.Click();
            }

        }

        public void UnGoogle()
        {
            Driver.StartActivity("com.whatsapp", ".gdrive.SettingsGoogleDrive");
            var element = AwaitText(Driver, "Резервное копирование", 10);
            if (element.Item1)
            {
                element.Item2.Click();
                var element2 = AwaitText(Driver, "Никогда", 10);
                if (element2.Item1) element2.Item2.Click(); 
            }
        }

        public bool LoadAvatar(string fileName)
        {
            string adb = BaseDir + "adb.exe";
            string args = "-s 127.0.0.1:215" + Index.ToString() + "3";
            args += " push \""+fileName+"\" \"/sdcard/WhatsApp/Media/WhatsApp Profile Photos/\"";
            Process.Start(adb, args).WaitForExit();
            try { 
            Driver.StartActivity("com.whatsapp", ".ProfileInfoActivity");
            AwaitElement(Driver, "Изменить фото", 20, "ImageButton", "content-desc").Item2.Click();
            AwaitElement(Driver, "Галерея").Item2.Click();
            AwaitElement(Driver, "WhatsApp Profile Fotos").Item2.Click();
            AwaitElement(Driver, "Фото", 20, "ImageView", "content-desc").Item2.Click();
            AwaitElement(Driver, "Готово").Item2.Click();
                return true;
            } catch {
                return false;
            }
        }

        public bool SendContent(string fileName,string text,string contentType="foto")
        {
            string adb = BaseDir + "adb.exe";
            string args = "-s 127.0.0.1:215" + Index.ToString() + "3";
            if (contentType.Equals("foto"))
            {
                args += " push \"" + fileName + "\"/sdcard/WhatsApp/Media/LoadedContent.jpg\"" ;
            }
            else
            {
                args += " push \"" + fileName + "\"/sdcard/Download/videofile."+Path.GetExtension(fileName)+"\"";
            }
            Process.Start(adb, args).WaitForExit();
            try
            {
                if (contentType.Equals("foto"))
                {
                    AwaitElement(Driver, "Прикрепить", 20, "ImageButton", "content-desc").Item2.Click();
                    AwaitElement(Driver, "Галерея", 20, "ImageView", "content-desc").Item2.Click();
                    AwaitElement(Driver, "Media"/*, 20, "ImageButton", "content-desc"*/).Item2.Click();
                    AwaitElement(Driver, "Фото", 20, "ImageView", "content-desc").Item2.Click();
                    AwaitElement(Driver, "Добавить подпись"/*, 20, "ImageButton", "content-desc"*/).Item2.Click();
                    AwaitElement(Driver, "Добавить подпись").Item2.SendKeys(text);
                    AwaitElement(Driver, "Отправить", 20, "ImageButton", "content-desc").Item2.Click();
                }
                else
                {
                    AwaitElement(Driver, "Прикрепить", 20, "ImageButton", "content-desc").Item2.Click();
                    AwaitElement(Driver, "Документ", 20, "ImageView", "content-desc").Item2.Click();
                    //AwaitElement(Driver, "Media"/*, 20, "ImageButton", "content-desc"*/).Item2.Click();
                    AwaitElement(Driver, Path.GetFileNameWithoutExtension(fileName)).Item2.Click();
                    AwaitElement(Driver, "Отправить", 20, "Button").Item2.Click();
                    AwaitElementClass(Driver, 20).Item2.SendKeys(text);
                    AwaitElement(Driver, "Отправить", 20, "ImageButton", "content-desc").Item2.Click();
                }
                return true;
            }
            catch { return false; }
        }
    }
}

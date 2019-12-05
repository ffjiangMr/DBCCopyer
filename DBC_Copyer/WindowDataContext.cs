namespace DBC_Copyer
{

    #region using directive 
    using Microsoft.Win32;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using log4net;
    using log4net.Appender;

    #endregion

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public static readonly String AttributeDefinitionPattern = "^BA_DEF_[ ]+((BU_)|(BO_)|(SG_)|(EV_))?[ ]+\"(\\w+)\"[ ]+(((INT)[ ]+([+|-]?\\d+)[ ]+([+|-]?\\d+))|((HEX)[ ]+([+|-]?\\d+)[ ]+([+|-]?\\d+))|((FLOAT)[ ]+([+|-]?\\d+.?\\d*)[ ]+([+|-]?\\d+.?\\d*))|(STRING)|((ENUM)[ ]+(\"((([^\"\\s])|([\\s\\u4e00-\\u9fa5]))*)\"([ ]*,\"((([^\"\\s])|([\\s\\u4e00-\\u9fa5]))*)\")*)))[ ]*;$";
        public static readonly String AttributeDefinitionRelPattern = "^BA_DEF_REL_[ ]+((BU_SG_REL_)|(BU_BO_REL_))[ ]+\"(\\w+)\"[ ]+(((INT)[ ]+([+|-]?\\d+)[ ]+([+|-]?\\d+))|((HEX)[ ]+([+|-]?\\d+)[ ]+([+|-]?\\d+))|((FLOAT)[ ]+([+|-]?\\d+.?\\d*)[ ]+([+|-]?\\d+.?\\d*))|(STRING)|((ENUM)[ ]+(\"((([^\"\\s])|([\\s\\u4e00-\\u9fa5]))*)\"([ ]*,\"((([^\"\\s])|([\\s\\u4e00-\\u9fa5]))*)\")*)))[ ]*;$";
        public static readonly String AttributeDefaultPattern = "^BA_DEF_DEF_[ ]+\"(\\w+)\"[ ]+\"?(([+|-]?\\d*.?\\d*)|((([^\"\\s])|([\\s\\u4e00-\\u9fa5]))*))\"?;$";
        public static readonly String AttributeDefaultRelPattern = "^BA_DEF_DEF_REL_[ ]+\"(\\w+)\"[ ]+\"?(([+|-]?\\d*.?\\d*)|((([^\"\\s])|([\\s\\u4e00-\\u9fa5]))*))\"?;$";
        public static readonly String AttributeDefinitionSymple = "BA_DEF_ ";
        public static readonly String AttributeDefaultSymple = "BA_DEF_DEF_ ";
        public static readonly String AttributeSymple = "BA_ ";
        public static readonly String CommentSymple = "CM_ ";
        public static readonly String ValueSymple = "VAL_ ";
        public static readonly String NodeSymple = "BU_: ";


        public ILog Logger = LogManager.GetLogger(typeof(MainWindow));

        public String TemplateFileName
        {
            get
            {
                return this.templateFileName;
            }
            set
            {
                this.templateFileName = value;
                OnNotifyPropertyChanged("TemplateFileName");
            }
        }

        public ObservableCollection<String> FileList
        {
            get
            {

                return this.fileList;
            }
            set
            {
                this.fileList = value;
                this.OnNotifyPropertyChanged("FileList");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SelectTemplate_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Func in.");
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "DBC File| *.dbc";
            if (fileDialog.ShowDialog() == true)
            {
                this.Logger.Info($"选择的模版文件为 : {fileDialog.FileName}");
                this.TemplateFileName = fileDialog.FileName;
                using (var reader = new StreamReader(this.TemplateFileName, Encoding.Default))
                {
                    this.Logger.Debug($"打开文件：{this.TemplateFileName}");
                    String content = reader.ReadLine();
                    while (content != null)
                    {
                        this.Logger.Debug($"读取内容为：{content}");
                        var attribute = Regex.Match(content.Trim(), AttributeDefinitionPattern);
                        var attributeRel = Regex.Match(content.Trim(), AttributeDefinitionRelPattern);
                        if ((attribute.Success == true) || (attributeRel.Success == true))
                        {
                            var dbcAttribute = new DBCAttribute()
                            {
                                Content = content,
                                Name = attribute.Success == true ? attribute.Groups[6].ToString() : attributeRel.Groups[4].ToString(),
                                ObjectType = attribute.Success == true ? attribute.Groups[1].ToString() : attributeRel.Groups[1].ToString(),
                                ValueType = attribute.Success == true ?
                                            attribute.Groups[9].ToString() + attribute.Groups[13].ToString() + attribute.Groups[17].ToString() + attribute.Groups[20].ToString() + attribute.Groups[22].ToString() :
                                            attributeRel.Groups[7].ToString() + attributeRel.Groups[11].ToString() + attributeRel.Groups[15].ToString() + attributeRel.Groups[18].ToString() + attributeRel.Groups[20].ToString(),
                            };
                            this.Logger.Info($"从模版文件中找到 {dbcAttribute.Name} 属性");
                            this.attributeList.Add(dbcAttribute);
                        }
                        else
                        {
                            attribute = Regex.Match(content.Trim(), AttributeDefaultPattern);
                            attributeRel = Regex.Match(content.Trim(), AttributeDefaultRelPattern);
                            var temp = attribute.Success == true ? attribute : attributeRel;
                            if (temp.Success == true)
                            {
                                this.attributeDefaultMap[temp.Groups[1].ToString()] = content;
                                this.Logger.Info($"从模版文件中找到 {temp.Groups[1].ToString()} 属性默认值");
                            }
                        }
                        content = reader.ReadLine();
                    }
                }
            }
            else
            {
                this.Logger.Warn("未选择模版文件！");
            }
            Logger.Info("Func out.");
        }

        private void SelectDBCFile_Click(object sender, RoutedEventArgs e)
        {
            this.Logger.Info("Func in.");
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Filter = "DBC File| *.dbc";
            this.FileList.Clear();
            if (fileDialog.ShowDialog() == true)
            {
                foreach (var item in fileDialog.FileNames)
                {
                    this.FileList.Add(item);
                    this.Logger.Info($"选择DBC 源文件 ：{item}");
                }
            }
            else
            {
                this.Logger.Warn("未选择dbc 源文件！");
            }
            this.Logger.Info("Func out.");
        }

        private void Execuit_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() =>
            {
                try
                {
                    foreach (var item in this.FileList)
                    {
                        this.Logger.Info($"准备向源文件 \" {item} \"中导入数据 ");
                        String dbcContent;
                        var attributeList = this.GetInsertAttribute(item, out dbcContent);
                        var dbcNewFile = item.Insert(item.LastIndexOf('.'), "_NeuSAR");
                        if (File.Exists(dbcNewFile) == true)
                        {
                            this.Logger.Error($"目标文件 {dbcNewFile} 已存在");
                            if (MessageBox.Show("目标文件已存在，是否继续?", "警告", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            {
                                this.Logger.Warn($"手动取消导入操作，原因：目标文件 {dbcNewFile} 已存在！");
                                continue;
                            }
                        }
                        using (var writer = new FileStream(dbcNewFile, FileMode.Create, FileAccess.Write))
                        {
                            foreach (var attributeName in attributeList)
                            {
                                this.Logger.Info($"向DBC源文件\" {item} \" 中 添加：{attributeName}属性");
                                this.InsertContent(attributeName, ref dbcContent);
                            }
                            var buffer = Encoding.Default.GetBytes(dbcContent);
                            writer.Write(buffer, 0, buffer.Length);
                        }
                        this.Logger.Info($"向源文件 \" {item} \"中导入数据完成，生成目标文件：{dbcNewFile} ");
                    }
                    MessageBox.Show("导入执行完成！", "提示", MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex.Message);
                }

            });
            task.Start();
        }

        private void OnNotifyPropertyChanged(String propertyName)
        {
            var temp = this.PropertyChanged;
            if (temp != null)
            {
                temp(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void InitLog()
        {
            this.Logger.Info("Func in.");
            var repositories = LogManager.GetAllRepositories();
            foreach (var item in repositories)
            {
                var appenders = item.GetAppenders();
                foreach (var appender in appenders)
                {
                    if (appender.GetType() == typeof(RollingFileAppender))
                    {
                        FileAppender tempAppender = appender as RollingFileAppender;
                        tempAppender.Encoding = Encoding.Default;
                        tempAppender.File = @"Log\" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss.fffff") + ".log";
                        tempAppender.ActivateOptions();
                    }
                }
            }
            this.Logger.Info("Func out.");
        }

        private void InsertContent(in String attributeName, ref String dbcContent)
        {
            this.Logger.Info("Func in.");
            try
            {
                String name = attributeName;
                var index = this.FindIndex(ref dbcContent, AttributeDefinitionSymple);
                dbcContent = dbcContent.Insert(index, (this.attributeList.Find(a => a.Name == name)).Content + Environment.NewLine);
                index = this.FindIndex(ref dbcContent, AttributeDefaultSymple);
                dbcContent = dbcContent.Insert(index, this.attributeDefaultMap[attributeName] + Environment.NewLine);
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex.Message);
            }
            this.Logger.Info("Func out.");
        }

        private Int32 FindIndex(ref String dbcContent, in String symble)
        {
            Int32 result = -1;
            var nodeIndex = dbcContent.IndexOf(NodeSymple) + 1;
            var subContent = dbcContent.Substring(nodeIndex);
            result = subContent.IndexOf(symble);
            if (result == -1)
            {
                this.Logger.Warn($"源文件中未定义：{symble} ");
                result = subContent.IndexOf(AttributeSymple);
                if (result == -1)
                {
                    this.Logger.Warn($"源文件中未定义：{AttributeSymple} ");
                    result = subContent.IndexOf(ValueSymple);
                    if (result == -1)
                    {
                        this.Logger.Warn($"源文件中未定义：{ValueSymple} ");
                        result = dbcContent.Length;
                    }
                    else
                    {
                        result += nodeIndex;
                    }
                }
                else
                {
                    result += nodeIndex;
                }
            }
            else
            {
                result += nodeIndex;
            }
            return result;
        }

        private List<String> GetInsertAttribute(in String filePath, out String dbcContent)
        {
            List<String> result = new List<String>();
            dbcContent = String.Empty;
            foreach (var attirbute in this.attributeList)
            {
                result.Add(attirbute.Name);
            }
            using (var reader = new StreamReader(filePath, Encoding.Default))
            {
                String content = reader.ReadLine();
                while (content != null)
                {
                    dbcContent += content;
                    dbcContent += Environment.NewLine;
                    var attribute = Regex.Match(content.Trim(), AttributeDefinitionPattern);
                    var attributeRel = Regex.Match(content.Trim(), AttributeDefinitionRelPattern);
                    var attributeName = attribute.Success ? attribute.Groups[6].ToString() : attributeRel.Groups[4].ToString();
                    if ((attribute.Success == true) || (attributeRel.Success == true))
                    {
                        this.Logger.Info($"从源文件\" {filePath} \" 中找到：{attributeName} 属性");
                        if (result.Contains(attributeName))
                        {
                            result.Remove(attributeName);
                            this.Logger.Warn($"DBC源文件\" {filePath} \" 中存在模版中同名属性：{attributeName},该属性跳过拷贝！");
                        }
                    }
                    content = reader.ReadLine();
                }
            }
            return result;
        }

        private String templateFileName;
        private ObservableCollection<String> fileList = new ObservableCollection<String>();
        private List<DBCAttribute> attributeList = new List<DBCAttribute>();
        private Dictionary<String, String> attributeDefaultMap = new Dictionary<String, String>();
    }
}

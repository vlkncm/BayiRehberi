using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace BayiRehberi
{
    internal class Dealer
    {
        public string Name, City, District, Phone, Address;
        public string[] Values { get { return new[] { Name, City, District, Phone, Address }; } }
    }

    internal static class Store
    {
        public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BayiRehberi");
        public static readonly string FilePath = Path.Combine(Folder, "bayiler.tsv");
        static string Clean(string value) { return (value ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ").Trim(); }

        public static List<Dealer> Load()
        {
            Directory.CreateDirectory(Folder);
            if (!File.Exists(FilePath)) Save(Seeds());
            var items = File.ReadAllLines(FilePath, Encoding.UTF8).Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).Select(line => {
                var p = line.Split('\t'); Array.Resize(ref p, 5);
                return new Dealer { Name=p[0], City=p[1], District=p[2], Phone=p[3], Address=p[4] };
            }).ToList();
            var unique = new List<Dealer>(); var seenPhones = new HashSet<string>(); var seenNames = new HashSet<string>();
            foreach (var item in items) { var phone=Digits(item.Phone); var key=Key(item.Name,item.City); if ((phone.Length>0 && seenPhones.Contains(phone)) || seenNames.Contains(key)) continue; unique.Add(item); if(phone.Length>0)seenPhones.Add(phone); seenNames.Add(key); }
            items = unique;
            var knownPhones = new HashSet<string>(items.Select(x => Digits(x.Phone)).Where(x => x.Length > 0));
            var knownNames = new HashSet<string>(items.Select(x => Key(x.Name, x.City)));
            foreach (var seed in Seeds())
            {
                var phone = Digits(seed.Phone); var key = Key(seed.Name, seed.City);
                if ((phone.Length > 0 && knownPhones.Contains(phone)) || knownNames.Contains(key)) continue;
                items.Add(seed); if (phone.Length > 0) knownPhones.Add(phone); knownNames.Add(key);
            }
            Save(items);
            return items;
        }

        public static void Save(IEnumerable<Dealer> items)
        {
            Directory.CreateDirectory(Folder);
            var lines = new List<string> { "Unvan\tSehir\tIlce\tTelefon\tAdres" };
            lines.AddRange(items.Select(x => string.Join("\t", x.Values.Select(Clean))));
            File.WriteAllLines(FilePath, lines, new UTF8Encoding(true));
        }

        static List<Dealer> Seeds()
        {
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(SeedData.Base64));
            return text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => { var p=line.TrimEnd('\r').Split('\t'); Array.Resize(ref p,5); return new Dealer{Name=p[0],City=p[1],District=p[2],Phone=p[3],Address=p[4]}; }).ToList();
        }
        static string Digits(string value) { var result=new string((value ?? "").Where(char.IsDigit).ToArray()); return result.Length==11&&result[0]=='0'?result.Substring(1):result; }
        static string Key(string name,string city) { return new string(((name??"")+"|"+(city??"")).ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray()); }
    }

    internal class EditForm : Form
    {
        readonly TextBox name=new TextBox(), city=new TextBox(), district=new TextBox(), phone=new TextBox(), address=new TextBox();
        public Dealer Result;
        public EditForm(Dealer item)
        {
            Text="Bayi Bilgileri"; Size=new Size(560,520); StartPosition=FormStartPosition.CenterParent; BackColor=Color.White; Font=new Font("Segoe UI",10); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
            var title=new Label{Text=item==null?"Yeni bayi ekle":"Bayi bilgilerini düzenle",Font=new Font("Segoe UI Semibold",18),AutoSize=true,Location=new Point(30,24)}; Controls.Add(title);
            AddField("Bayi / Firma",name,78); AddField("İl",city,142); AddField("Semt / İlçe",district,206); AddField("Telefon",phone,270); AddField("Adres",address,334); address.Height=55; address.Multiline=true;
            if(item!=null){name.Text=item.Name;city.Text=item.City;district.Text=item.District;phone.Text=item.Phone;address.Text=item.Address;}
            var cancel=Btn("Vazgeç",350,420,Color.FromArgb(235,238,242),Color.FromArgb(35,44,58)); cancel.DialogResult=DialogResult.Cancel;
            var save=Btn("Kaydet",438,420,Color.FromArgb(21,92,117),Color.White); save.Click+=(s,e)=>{if(string.IsNullOrWhiteSpace(name.Text)||string.IsNullOrWhiteSpace(city.Text)){MessageBox.Show("Bayi ve il alanları zorunludur.","Eksik bilgi");return;}Result=new Dealer{Name=name.Text.Trim(),City=city.Text.Trim(),District=district.Text.Trim(),Phone=phone.Text.Trim(),Address=address.Text.Trim()};DialogResult=DialogResult.OK;Close();};
            Controls.Add(cancel);Controls.Add(save); AcceptButton=save;CancelButton=cancel;
        }
        void AddField(string label,TextBox box,int y){Controls.Add(new Label{Text=label,AutoSize=true,Location=new Point(30,y),ForeColor=Color.FromArgb(80,90,105)});box.Location=new Point(30,y+23);box.Width=480;box.Font=new Font("Segoe UI",11);Controls.Add(box);}
        Button Btn(string text,int x,int y,Color bg,Color fg){return new Button{Text=text,Location=new Point(x,y),Size=new Size(72,38),FlatStyle=FlatStyle.Flat,BackColor=bg,ForeColor=fg};}
    }

    internal class MainForm : Form
    {
        readonly ComboBox cities=new ComboBox(); readonly TextBox search=new TextBox(); readonly DataGridView grid=new DataGridView(); readonly Label count=new Label(); List<Dealer> all; List<Dealer> shown;
        readonly Color navy=Color.FromArgb(25,38,54), teal=Color.FromArgb(21,92,117), pale=Color.FromArgb(244,247,249);
        public MainForm()
        {
            Text="Bayi Rehberi"; MinimumSize=new Size(950,600); Size=new Size(1180,720); StartPosition=FormStartPosition.CenterScreen; BackColor=pale; Font=new Font("Segoe UI",10);
            var assembly=Assembly.GetExecutingAssembly();
            using(var iconStream=assembly.GetManifestResourceStream("BayiRehberi.ico")){if(iconStream!=null)Icon=new Icon(iconStream);}
            var header=new Panel{Dock=DockStyle.Top,Height=92,BackColor=navy}; header.Controls.Add(new Label{Text="BAYİ ARAMA",ForeColor=Color.White,Font=new Font("Segoe UI Semibold",25),AutoSize=true,Location=new Point(28,27)});
            var logoStream=assembly.GetManifestResourceStream("saatDunyasiLogo.jpg"); if(logoStream!=null){var logo=new PictureBox{Dock=DockStyle.Right,Width=240,Padding=new Padding(18,12,24,12),BackColor=Color.White,SizeMode=PictureBoxSizeMode.Zoom,Image=Image.FromStream(logoStream)};header.Controls.Add(logo);}
            Controls.Add(header);
            var filters=new Panel{Dock=DockStyle.Top,Height=142,Padding=new Padding(28,18,28,14),BackColor=Color.White}; Controls.Add(filters); filters.BringToFront();
            cities.DropDownStyle=ComboBoxStyle.DropDownList; cities.Width=210; cities.Location=new Point(28,28); cities.Font=new Font("Segoe UI",11); cities.SelectedIndexChanged+=(s,e)=>RefreshGrid(); filters.Controls.Add(cities);
            search.Location=new Point(258,28);search.Width=420;search.Font=new Font("Segoe UI",11);search.TextChanged+=(s,e)=>RefreshGrid();filters.Controls.Add(search);
            var hint=new Label{Text="Bayi, semt veya telefon ara",ForeColor=Color.Gray,AutoSize=true,Location=new Point(260,10)};filters.Controls.Add(hint);
            var add=Button("+ Yeni Bayi",teal,Color.White);add.Location=new Point(28,82);add.Click+=(s,e)=>Edit(null);filters.Controls.Add(add);
            var edit=Button("Düzenle",Color.FromArgb(228,234,238),navy);edit.Location=new Point(150,82);edit.Click+=(s,e)=>Edit(Selected());filters.Controls.Add(edit);
            var remove=Button("Sil",Color.FromArgb(250,230,230),Color.FromArgb(150,45,45));remove.Location=new Point(272,82);remove.Click+=(s,e)=>Delete();filters.Controls.Add(remove);
            count.Dock=DockStyle.Bottom;count.Height=34;count.Padding=new Padding(28,7,0,0);count.ForeColor=Color.FromArgb(90,100,110);Controls.Add(count);
            SetupGrid(); Controls.Add(grid); grid.BringToFront(); all=Store.Load(); LoadCities("İstanbul");
        }
        Button Button(string text,Color bg,Color fg){return new Button{Text=text,Size=new Size(110,40),FlatStyle=FlatStyle.Flat,BackColor=bg,ForeColor=fg,Cursor=Cursors.Hand};}
        void SetupGrid(){grid.Dock=DockStyle.Fill;grid.BackgroundColor=pale;grid.BorderStyle=BorderStyle.None;grid.RowHeadersVisible=false;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.ReadOnly=true;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.MultiSelect=false;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;grid.ColumnHeadersHeight=44;grid.RowTemplate.Height=42;grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(225,232,236);grid.ColumnHeadersDefaultCellStyle.ForeColor=navy;grid.DefaultCellStyle.BackColor=Color.White;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(211,231,238);grid.DefaultCellStyle.SelectionForeColor=navy;grid.DefaultCellStyle.Padding=new Padding(6);grid.Columns.Add("name","Bayi / Firma");grid.Columns.Add("district","Semt / İlçe");grid.Columns.Add("city","İl");grid.Columns.Add("phone","Telefon");grid.Columns.Add("address","Adres");grid.Columns[0].FillWeight=28;grid.Columns[1].FillWeight=13;grid.Columns[2].FillWeight=11;grid.Columns[3].FillWeight=15;grid.Columns[4].FillWeight=33;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0)Edit(Selected());};}
        void LoadCities(string selected){var list=all.Select(x=>x.City).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(x=>x).ToList();cities.Items.Clear();cities.Items.Add("Tüm İller");foreach(var c in list)cities.Items.Add(c);var idx=cities.FindStringExact(selected);cities.SelectedIndex=idx>=0?idx:0;}
        void RefreshGrid(){if(all==null)return;var city=cities.SelectedItem as string??"Tüm İller";var q=search.Text.Trim();shown=all.Where(x=>(city=="Tüm İller"||string.Equals(x.City,city,StringComparison.CurrentCultureIgnoreCase))&&(q.Length==0||(x.Name+" "+x.City+" "+x.District+" "+x.Phone+" "+x.Address).IndexOf(q,StringComparison.CurrentCultureIgnoreCase)>=0)).OrderBy(x=>x.District).ThenBy(x=>x.Name).ToList();grid.Rows.Clear();foreach(var d in shown)grid.Rows.Add(d.Name,d.District,d.City,d.Phone,d.Address);count.Text=shown.Count+" bayi gösteriliyor";}
        Dealer Selected(){return grid.CurrentRow==null||shown==null||grid.CurrentRow.Index>=shown.Count?null:shown[grid.CurrentRow.Index];}
        void Edit(Dealer dealer){var f=new EditForm(dealer);if(f.ShowDialog(this)!=DialogResult.OK)return;if(dealer==null)all.Add(f.Result);else{dealer.Name=f.Result.Name;dealer.City=f.Result.City;dealer.District=f.Result.District;dealer.Phone=f.Result.Phone;dealer.Address=f.Result.Address;}Store.Save(all);LoadCities(f.Result.City);}
        void Delete(){var d=Selected();if(d==null)return;if(MessageBox.Show(d.Name+" kaydı silinsin mi?","Kaydı sil",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;all.Remove(d);Store.Save(all);LoadCities(cities.SelectedItem as string);}
    }

    internal static class Program
    {
        [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm());}
    }
}

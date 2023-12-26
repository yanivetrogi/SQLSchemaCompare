//ttjjjj::    ;;jjjjii  iijjjjii      ;;GGGGGGii      iijjjjjjjjjjjjjj::          
//GG####ff    jj####LL  GG####LL    ::GG######DD,,    LL############WWtt          
//DD######..  ff####LL  GG####GG  ..WW##########WW::  LL##############tt          
//DD######jj  ff####LL  GG####GG  ff####WWGGKK####LL  LL####WWDDDDDDGGii          
//DD######WW  ff####LL  GG####GG  WW####..    WW##WW  LL####GG                    
//DD########ttjj####GG;;DD####GGiiKK##DD      iiffffiiGGKK##KKtttttt..            
//DD####WW##DDLL##KKEEEEKK####KKEEEEKKGG            EEEEEE##########::            
//DD####GG####GG##KKEEEEKK####KKEEEEWWLL            EEKKKK##########::            
//DD####ttDD######KKEELLKK####KKGGKKWWGG      ..::::LLEEKK##WWDDDDDD::            
//DD####ttii########LL  GG####LL..####WW      DD####  LL####GG                    
//DD####tt  WW######LL  GG####GG  GG####jj,,tt####DD  LL####DDiiiiiiii..          
//DD####tt  tt######LL  GG####GG  ii##############tt  LL##############tt          
//DD####tt    WW####LL  GG####GG    ff##########GG    LL##############tt          
//iitttt::    ,,tttt,,  ;;tttt;;      ::ttttttii      ;;ttttttttttttii::          
/// <creator>lirons</creator>
/// <creationdate>11/09/2006 10:06:36</creationdate>
/// <summary>
/// 
/// </summary>  

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.Data;

namespace ToolBox
{
    
    public class BUS : Dictionary<string,object>
    {
        
        public event EventHandler OnUpdate;
        #region Prv Members

        #endregion
        private BUS _parent;

       

        #region Gets & Sets
        public BUS Parent
        {
            get { return _parent; }
            set { 
                _parent = value;
                if (_parent!=null)
                {
                    _parent.OnUpdate += new EventHandler(_parent_OnUpdate);
                }
                
                }
        }

        void _parent_OnUpdate(object sender, EventArgs e)
        {
            ItemsUpdate(sender.ToString());
        }
        #endregion

        #region Ctors

        public BUS()
        {
            Parent = null;
        }
        #endregion

        #region Prv Funcs

        private void ItemsUpdate(string key)
        {
            if (OnUpdate!=null)
            {
                OnUpdate(key,null);

            }
        }
        #endregion

        #region Public Funcs

        public bool HasValue(string key, string val)
        {
            bool res = false;
            if (this.ContainsKey(key))
            {
                if (this[key]==val)
                {
                    res = true;
                }
            }
            return res;
        }

        public void InsertSingel(string key)
        {
            int q = 1;
            Insert(key, q);
        }
                

        public void Insert(string key, string val)
        {
               
                //if (ContainsKey(key))
                //{
                //   Remove(key);
                //}
                //Add(key, val);
                //ItemsUpdate(key);
            Insert(key, val,false);
        }
        public void Insert(string key, string val,bool generationBack)
        {

            //if (ContainsKey(key))
            //{
            //   Remove(key);
            //}
            //Add(key, val);
            //ItemsUpdate(key);
            Insert(key, (object)val, generationBack);
        }
        public void Insert(string key, object val)
        {
            Insert(key, val, false);
        }


        public void Insert(string key, object val, bool generationBack)
        {
            if (!generationBack)
            {
                if (ContainsKey(key))
                {
                    Remove(key);
                }
                Add(key, val);
                ItemsUpdate(key);
            }
            else
            {
                if (_parent!=null)
                {
                    _parent.Insert(key, val);
                }
            }
           
        }

        public void CopyFromBus(BUS orgBus)
        {
            foreach (KeyValuePair<string,object> kvp in orgBus)
            {
                Insert(kvp.Key, kvp.Value);
            }
            
        }

        public List<KeyValuePair<string,string>> GetAllKvp()
        {
            List<KeyValuePair<string, string>> res = new List<KeyValuePair<string, string>>();

            foreach (KeyValuePair<string,object> oKvp in this)
            {
                KeyValuePair<string, string> nKvp= new KeyValuePair<string, string>(oKvp.Key,oKvp.Value.ToString());
            
                res.Add(nKvp);
            }

            if (Parent!=null)
            {
                res.AddRange(Parent.GetAllKvp());          
            }

            return res;
        }


        public string ReturnValue(string key)
        {
            return ReturnValue(key, true);
        }

        public string ReturnValue(string key, bool withGeneration)
        {
      
            string res = "";
            if (this.ContainsKey(key))
            {
                res = this[key].ToString();
                return res;
            }
            if (withGeneration)
            {
                
                if (Parent != null)
                {
                    res = Parent.ReturnValue(key);
                }
            }
            return res;
        }

        public BUS ReturnBus(string key)
        {
            return ReturnBus(key, true);
        }

        public BUS ReturnBus(string key, bool withGeneration)
        {

            BUS res = new BUS();
            if (this.ContainsKey(key))
            {
                res = base[key] as BUS;
                return res;
            }
            if (withGeneration)
            {

                if (Parent != null)
                {
                    res = Parent.ReturnBus(key);
                }
            }
            return res;
        }


        public int GetInt(string key)
        {
            try
            {
                return Convert.ToInt32(ReturnObj(key, false));
            }
            catch (Exception)
            {

                return 0;
            }
            
        }



        public object ReturnObj(string key)
        {
            return ReturnObj(key, true);
        }

        public object ReturnObj(string key, bool withGeneration)
        {

            object res = null;
            if (this.ContainsKey(key))
            {
                res = base[key];
                return res;
            }
            if (withGeneration)
            {

                if (Parent != null)
                {
                    res = Parent.ReturnObj(key);
                }
            }
            return res;
        }




        public void RemoveValue(string key)
        {
            if (this.ContainsKey(key))
            {
                this.Remove(key);
            }
            ItemsUpdate(key);
        }

        public void RemoveKeyIfEquelValue(string key, string val, bool withGenerations)
        {
            if (this.ContainsKey(key))
            {
                if (this[key]==val)
                {
                    this.Remove(key);
                }
               
            }
            if (withGenerations)
            {

                if (Parent != null)
                {
                    Parent.RemoveKeyIfEquelValue(key,val,withGenerations);
                }
            }
            ItemsUpdate(key);
        }

        public void UpdateInt(string key, int val)
        {
            
            if (ContainsKey(key))
            {
                val += Convert.ToInt32(this[key]);
                
            }
            Insert(key, val, false);
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool xsl)
        {
            StringBuilder sb = new StringBuilder();
            List<KeyValuePair<string, string>> kvps = GetAllKvp();
            foreach (KeyValuePair<string, string> kvp in kvps)
            {
                sb.Append(kvp.Key);
                sb.Append(":");
                sb.Append(kvp.Value);
                sb.Append(";");
                //if (xsl)
                //{
                //    //sb.Append(@"&#13;&#10;");
                //}
                //else
                {
                    sb.Append(Environment.NewLine);
                }
               
               
            }
            return sb.ToString();
        }

       
	public new string this[string index]
	{
		get { return  base[index].ToString(); }
        set { this.Insert(index, value); }
	}
	

        #endregion
    }

}

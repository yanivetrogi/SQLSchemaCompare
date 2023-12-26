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
/// <creationdate>25/02/2007 17:19:07</creationdate>
/// <summary>
/// 
/// </summary>  

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.Data;


namespace SQLSchemaCompare
{
    static class ComputerIDGnr
    {
        #region Prv Members

        #endregion

        #region Gets & Sets

        #endregion

        #region Ctors

        //private ComputerIDGnr()
        //{
        //    SystemInfo.UseBaseBoardManufacturer = true;
        //    SystemInfo.UseBaseBoardProduct = true;
        //    SystemInfo.UseBiosManufacturer = true;
        //    SystemInfo.UseBiosVersion = false;
        //    SystemInfo.UseDiskDriveSignature = true;
        //    SystemInfo.UsePhysicalMediaSerialNumber = true;
        //    SystemInfo.UseProcessorID = true;
        //    SystemInfo.UseVideoControllerCaption = false;
        //    SystemInfo.UseWindowsSerialNumber = true;
        //}
        #endregion

        #region Prv Funcs





        #endregion

        #region Public Funcs

        public static string GetComputerID()
        {
            SystemInfo.UseBaseBoardManufacturer = false;
            SystemInfo.UseBaseBoardProduct = false;
            SystemInfo.UseBiosManufacturer = false;
            SystemInfo.UseBiosVersion = false;
            SystemInfo.UseDiskDriveSignature = false;
            SystemInfo.UsePhysicalMediaSerialNumber = false;
            SystemInfo.UseProcessorID = true;
            SystemInfo.UseVideoControllerCaption = false;
            SystemInfo.UseWindowsSerialNumber = false;

            string nq = SystemInfo.GetSystemInfo("TE") + Program.ProductName;
            nq = Encryption.Boring(Encryption.InverseByBase(nq, 5));
            return nq.Substring(0,40);
        }


        #endregion
    }

}

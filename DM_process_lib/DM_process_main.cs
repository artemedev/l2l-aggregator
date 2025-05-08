namespace DM_process_NS
{
    using DM_wraper_lib;
    public class DM_process()
    {
        MainProces _MP = new MainProces();
        public void Init(DM_recogn_wraper dM_recogn_wraper)
        {
            _DMP.init_readConfig();
            _DMP.updateWraper(dM_recogn_wraper);
            Console.WriteLine("DM_process - initialisation");
            //Подписались на событие
            dM_recogn_wraper.libTakeShot += _MP.MP_TakeShot;
            dM_recogn_wraper.libUpdateParams += _DMP.update_DMP;
            dM_recogn_wraper.libUpdatePrintPattern += _DMP.update_PP;
            //set parameters
            dM_recogn_wraper.initForLib(_DMP._dmp);
        }
    }
}
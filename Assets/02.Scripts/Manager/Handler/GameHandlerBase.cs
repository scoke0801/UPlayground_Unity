namespace UPlayGround.Manager.Handler
{
    public abstract class GameHandlerBase
    {
        public virtual void Init(){}
        
        public virtual void AfterInit(){}
        public virtual void Dispose(){}
        
        public virtual void Update(){}
        
        public virtual void FixedUpdate() {}
        
    }
}
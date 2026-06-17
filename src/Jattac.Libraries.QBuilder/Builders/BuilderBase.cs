namespace Jattac.Libraries.QBuilder.Builders
{
    public abstract class BuilderBase
    {
        public BuilderBase(QBuilder qBuilder)
        {
            QBuilder = qBuilder;
        }

        protected QBuilder QBuilder { get; }

        public virtual QBuilder Then()
        {
            return QBuilder;
        }
    }
}
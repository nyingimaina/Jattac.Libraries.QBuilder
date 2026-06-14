namespace Jattac.Libraries.QBuilder.Builders
{
    using Rocket.Libraries.Validation.Services;

    public abstract class BuilderBase
    {
        public BuilderBase(QBuilder qBuilder)
        {
            QBuilder = qBuilder;
        }

        protected QBuilder QBuilder { get; }

        protected DataValidator DataValidator { get; } = new DataValidator();

        public virtual QBuilder Then()
        {
            return QBuilder;
        }
    }
}
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDReflectionProbe : HDProbe2
    {
        public struct ProbeCaptureProperties
        {
            public CaptureProperties common;
            public int cubemapSize;
        }

        public ProbeCaptureProperties captureSettings;

        public override Hash128 ComputeBakePropertyHashes()
        {
            var hash = new Hash128();
            HashUtilities.QuantisedVectorHash(ref captureSettings.common.position, ref hash);
            return hash;
        }
    }
}

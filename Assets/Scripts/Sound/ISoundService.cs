using General;

namespace Sound
{
    public interface ISoundService: IService
    {
        void PlaySound(ClipName clipName, float delay = 0);
    }
}
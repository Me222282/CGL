using System.IO;
using Zene.Graphics;
using Zene.Structs;

namespace cgl
{
    public class BoolShader : BaseShaderProgram, IDrawingShader
    {
        public BoolShader()
        {
            Create(File.ReadAllText("shaders/BoolVert.shader"),
                File.ReadAllText("shaders/BoolFrag.shader"),
                "uTextureSlot", "matrix");

            _m2m3 = Matrix.Identity * Matrix.Identity;
            _m1Mm2m3 = Matrix.Identity * _m2m3;

            SetUniform(Uniforms[1], Matrix.Identity);
            SetUniform(Uniforms[0], 0);
        }

        public ITexture Texture { get; set; }

        public override IMatrix Matrix1
        {
            get => _m1Mm2m3.Left;
            set => _m1Mm2m3.Left = value;
        }
        public override IMatrix Matrix2
        {
            get => _m2m3.Left;
            set => _m2m3.Left = value;
        }
        public override IMatrix Matrix3
        {
            get => _m2m3.Right;
            set => _m2m3.Right = value;
        }

        private readonly MultiplyMatrix _m1Mm2m3;
        private readonly MultiplyMatrix _m2m3;
        public override void PrepareDraw()
        {
            SetUniform(Uniforms[1], _m1Mm2m3);
            Texture?.Bind(0);
        }

        protected override void Dispose(bool dispose)
        {
            base.Dispose(dispose);

            State.CurrentContext.RemoveTrack(this);
        }
        /// <summary>
        /// Gets the instance of the <see cref="BoolShader"/> for this <see cref="GraphicsContext"/>.
        /// </summary>
        /// <returns></returns>
        public static BoolShader GetInstance() => GetInstance<BoolShader>();
    }
}
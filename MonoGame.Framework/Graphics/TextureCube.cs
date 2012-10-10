using System;
using System.Runtime.InteropServices;

#if MONOMAC
using MonoMac.OpenGL;
#elif WINDOWS || LINUX
using OpenTK.Graphics.OpenGL;
#elif EMBEDDED
using OpenTK.Graphics.ES20;
using GL = OpenTK.Graphics.ES20.GL;
using BlendEquationMode = OpenTK.Graphics.ES20.BlendEquationMode;
using BlendingFactorSrc = OpenTK.Graphics.ES20.BlendingFactorSrc;
using BlendingFactorDest = OpenTK.Graphics.ES20.BlendingFactorDest;
using VertexAttribPointerType = OpenTK.Graphics.ES20.VertexAttribPointerType;
using PixelInternalFormat = OpenTK.Graphics.ES20.PixelInternalFormat;
using PixelType = OpenTK.Graphics.ES20.PixelType;
using PixelFormat = OpenTK.Graphics.ES20.PixelFormat;
using VertexPointerType = OpenTK.Graphics.ES20.All;
using ColorPointerType = OpenTK.Graphics.ES20.All;
using NormalPointerType = OpenTK.Graphics.ES20.All;
using TexCoordPointerType = OpenTK.Graphics.ES20.All;
using TextureTarget = OpenTK.Graphics.ES20.TextureTarget;
#elif PSS
using Sce.PlayStation.Core.Graphics;
#elif WINRT
// TODO
#else
using OpenTK.Graphics.ES20;
#if IPHONE || ANDROID
using PixelInternalFormat = OpenTK.Graphics.ES20.All;
using PixelFormat = OpenTK.Graphics.ES20.All;
using PixelType = OpenTK.Graphics.ES20.All;
using TextureTarget = OpenTK.Graphics.ES20.All;
using TextureParameterName = OpenTK.Graphics.ES20.All;
using TextureMinFilter = OpenTK.Graphics.ES20.All;
#endif
#endif

namespace Microsoft.Xna.Framework.Graphics
{
	public class TextureCube : Texture
	{
		protected int size;

        public int Size
        {
            get
            {
                return size;
            }
        }
		
#if WINRT

#elif PSS
		//TODO
#else
		PixelInternalFormat glInternalFormat;
		PixelFormat glFormat;
		PixelType glType;
#endif
		
		public TextureCube (GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
		{
			
			this.size = size;
			this.levelCount = 1;

#if WINRT

#elif PSS
			//TODO
#else
			this.glTarget = TextureTarget.TextureCubeMap;

#if IPHONE || ANDROID
			GL.GenTextures(1, ref this.glTexture);
#else
			GL.GenTextures(1, out this.glTexture);
#endif
            GraphicsExtensions.CheckGLError();
            GL.BindTexture(TextureTarget.TextureCubeMap, this.glTexture);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
			                mipMap ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Linear);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
			                (int)TextureMagFilter.Linear);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
			                (int)TextureWrapMode.ClampToEdge);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
			                (int)TextureWrapMode.ClampToEdge);
            GraphicsExtensions.CheckGLError();


			format.GetGLFormat (out glInternalFormat, out glFormat, out glType);
			
			for (int i=0; i<6; i++) {
				TextureTarget target = GetGLCubeFace((CubeMapFace)i);

				if (glFormat == (PixelFormat)All.CompressedTextureFormats) {
					throw new NotImplementedException();
				} else {
#if IPHONE || ANDROID
					GL.TexImage2D (target, 0, (int)glInternalFormat, size, size, 0, glFormat, glType, IntPtr.Zero);
#else
					GL.TexImage2D (target, 0, glInternalFormat, size, size, 0, glFormat, glType, IntPtr.Zero);
#endif
                    GraphicsExtensions.CheckGLError();
                }
			}
			
			if (mipMap)
			{
#if IPHONE || ANDROID || EMBEDDED
				GL.GenerateMipmap(TextureTarget.TextureCubeMap);
#else
				GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.GenerateMipmap, (int)All.True);
#endif
                GraphicsExtensions.CheckGLError();

				int v = this.size;
				while (v > 1)
				{
					v /= 2;
					this.levelCount++;
				}
			}
#endif			
		}

        /// <summary>
        /// Gets a copy of cube texture data specifying a cubemap face.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cubeMapFace">The cube map face.</param>
        /// <param name="data">The data.</param>
        public void GetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
        {
            //FIXME Does not compile on Android or iOS
#if MONOMAC
            TextureTarget target = GetGLCubeFace(cubeMapFace);
            GL.BindTexture(target, this.glTexture);
            // 4 bytes per pixel
            if (data.Length < size * size * 4)
                throw new ArgumentException("data");

            GL.GetTexImage<T>(target, 0, PixelFormat.Bgra,
                PixelType.UnsignedByte, data);
#else
			throw new NotImplementedException();
#endif
        }

		public void SetData<T> (CubeMapFace face, T[] data) where T : struct
		{
			SetData<T> (face, 0, null, data, 0, data.Length);
		}

		public void SetData<T> (CubeMapFace face, T[] data,
								int startIndex, int elementCount) where T : struct
		{
			SetData<T> (face, 0, null, data, startIndex, elementCount);
		}
		
		public void SetData<T>(CubeMapFace face, int level, Rectangle? rect,
		                       T[] data, int startIndex, int elementCount) where T : struct
		{
            if (data == null) 
                throw new ArgumentNullException("data");

            var elementSizeInByte = Marshal.SizeOf(typeof(T));
			var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInByte);
			
			var xOffset = 0;
			var yOffset = 0;
			var width = Math.Max (1, this.size >> level);
			var height = Math.Max (1, this.size >> level);
			
			if (rect.HasValue)
			{
				xOffset = rect.Value.X;
				yOffset = rect.Value.Y;
				width = rect.Value.Width;
				height = rect.Value.Height;
			}
			
#if WINRT

#elif PSS
			//TODO
#else
			GL.BindTexture (TextureTarget.TextureCubeMap, this.glTexture);
            GraphicsExtensions.CheckGLError();

			TextureTarget target = GetGLCubeFace(face);
			if (glFormat == (PixelFormat)All.CompressedTextureFormats) {
				throw new NotImplementedException();
			} else {
				GL.TexSubImage2D(target, level, xOffset, yOffset, width, height, glFormat, glType, dataPtr);
                GraphicsExtensions.CheckGLError();
            }
#endif			
			dataHandle.Free ();
		}
		
#if !WINRT && !PSS
		private TextureTarget GetGLCubeFace(CubeMapFace face) {
			switch (face) {
			case CubeMapFace.PositiveX: return TextureTarget.TextureCubeMapPositiveX;
			case CubeMapFace.NegativeX: return TextureTarget.TextureCubeMapNegativeX;
			case CubeMapFace.PositiveY: return TextureTarget.TextureCubeMapPositiveY;
			case CubeMapFace.NegativeY: return TextureTarget.TextureCubeMapNegativeY;
			case CubeMapFace.PositiveZ: return TextureTarget.TextureCubeMapPositiveZ;
			case CubeMapFace.NegativeZ: return TextureTarget.TextureCubeMapNegativeZ;
			}
			throw new ArgumentException();
		}
#endif

	}
}


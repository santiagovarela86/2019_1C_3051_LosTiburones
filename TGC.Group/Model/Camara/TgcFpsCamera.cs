﻿using Microsoft.DirectX.DirectInput;
using System.Drawing;
using System.Windows.Forms;
using TGC.Core.Camara;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;

namespace TGC.Group.Model.Camara
{
    /// <summary>
    ///     Camara en primera persona que utiliza matrices de rotacion, solo almacena las rotaciones en updown y costados.
    ///     Ref: http://www.riemers.net/eng/Tutorials/XNA/Csharp/Series4/Mouse_camera.php
    ///     Autor: Rodrigo Garcia.
    /// </summary>
    public class TgcFpsCamera : TgcCamera
    {
        /// <summary>
        ///  Centro del mouse 2D para ocultarlo
        /// </summary>
        private readonly Point mouseCenter;

        /// <summary>
        ///  Se mantiene la matriz rotacion para no hacer este calculo cada vez.
        /// </summary>
        private TGCMatrix cameraRotation;

        /// <summary>
        ///  Direction view se calcula a partir de donde se quiere ver con la camara inicialmente. por defecto se ve en -Z.
        /// </summary>
        private TGCVector3 directionView;

        //No hace falta la base ya que siempre es la misma, la base se arma segun las rotaciones de esto costados y updown.
        private float leftrightRot;

        /// <summary>
        ///
        /// </summary>
        private float updownRot;

        /// <summary>
        ///  Se traba la camara, se utiliza para ocultar el puntero del mouse y manejar la rotacion de la camara.
        /// </summary>
        private bool lockCam = true;

        /// <summary>
        ///     Posicion de la camara
        /// </summary>
        private TGCVector3 positionEye;

        private bool habiaSalido = false;

        private GameModel gmodel;

        /// <summary>
        ///     Constructor de la camara a partir de un TgcD3dInput el cual ya tiene por default el positionEye (0,0,0), el mouseCenter a partir del centro del a pantalla, RotationSpeed 1.0f,
        ///     MovementSpeed y JumpSpeed 500f, el directionView (0,0,-1)
        /// </summary>
        /// <param name="input"></param>
        public TgcFpsCamera(GameModel gmodel)
        {
            this.Input = gmodel.Input;
            this.positionEye = TGCVector3.Empty;
            this.mouseCenter = new Point(D3DDevice.Instance.Device.Viewport.Width / 2, D3DDevice.Instance.Device.Viewport.Height / 2);
            this.RotationSpeed = 0.1f;
            this.MovementSpeed = 500f;
            this.JumpSpeed = 500f;
            this.directionView = new TGCVector3(0, 0, -1);
            this.leftrightRot = FastMath.PI_HALF;
            this.updownRot = -FastMath.PI / 10.0f;
            this.cameraRotation = TGCMatrix.RotationX(updownRot) * TGCMatrix.RotationY(leftrightRot);
            this.gmodel = gmodel;
        }

        /// <summary>
        ///     Constructor de la camara a partir de un TgcD3dInput y un positionEye. Los atributos mouseCenter a partir del centro del a pantalla, RotationSpeed 1.0f,
        ///     MovementSpeed y JumpSpeed 500f, el directionView (0,0,-1)
        /// </summary>
        /// <param name="positionEye"></param>
        /// <param name="input"></param>
        public TgcFpsCamera(TGCVector3 positionEye, GameModel gmodel) : this(gmodel)
        {
            this.positionEye = positionEye;
        }

        /// <summary>
        ///  Constructor de la camara a partir de un TgcD3dInput y un positionEye, moveSpeed y jumpSpeed. Los atributos mouseCenter a partir del centro del a pantalla, RotationSpeed 1.0f,
        ///  el directionView (0,0,-1)
        /// </summary>
        /// <param name="positionEye"></param>
        /// <param name="moveSpeed"></param>
        /// <param name="jumpSpeed"></param>
        /// <param name="input"></param>
        public TgcFpsCamera(TGCVector3 positionEye, float moveSpeed, float jumpSpeed, GameModel gmodel)
            : this(positionEye, gmodel)
        {
            this.MovementSpeed = moveSpeed;
            this.JumpSpeed = jumpSpeed;
        }

        /// <summary>
        /// Constructor de la camara a partir de un TgcD3dInput y un positionEye, moveSpeed, jumpSpeed y rotationSpeed. Los atributos mouseCenter a partir del centro del a pantalla,
        ///  el directionView (0,0,-1)
        /// </summary>
        /// <param name="positionEye"></param>
        /// <param name="moveSpeed"></param>
        /// <param name="jumpSpeed"></param>
        /// <param name="rotationSpeed"></param>
        /// <param name="input"></param>
        public TgcFpsCamera(TGCVector3 positionEye, float moveSpeed, float jumpSpeed, float rotationSpeed, GameModel gmodel)
            : this(positionEye, moveSpeed, jumpSpeed, gmodel)
        {
            this.RotationSpeed = rotationSpeed;
        }

        private TgcD3dInput Input { get; }

        /// <summary>
        ///  Condicion para trabar y destrabar la camara y ocultar el puntero de mouse.
        /// </summary>
        public bool LockCam
        {
            get { return lockCam; }
            set
            {
                if (!lockCam && value)
                {
                    Cursor.Position = mouseCenter;

                    Cursor.Hide();
                }
                if (lockCam && !value)
                    Cursor.Show();
                lockCam = value;
            }
        }

        /// <summary>
        ///  Velocidad de movimiento
        /// </summary>
        public float MovementSpeed { get; set; }

        /// <summary>
        ///  Velocidad de rotacion
        /// </summary>
        public float RotationSpeed { get; set; }

        /// <summary>
        ///  Velocidad de Salto
        /// </summary>
        public float JumpSpeed { get; set; }

        /// <summary>
        ///     Cuando se elimina esto hay que desbloquear la camera.
        /// </summary>
        ~TgcFpsCamera()
        {
            LockCam = false;
        }

        /// <summary>
        ///     Realiza un update de la camara a partir del elapsedTime, actualizando Position,LookAt y UpVector.
        ///     Presenta movimientos basicos a partir de input de teclado W, A, S, D, Espacio, Control y rotraciones con el mouse.
        /// </summary>
        /// <param name="elapsedTime"></param>
        public override void UpdateCamera(float elapsedTime)
        {
            var moveVector = TGCVector3.Empty;

            if (Input.keyPressed(Key.Escape) && !gmodel.InterfazCrafting.Activo && !gmodel.InterfazInventario.Activo){
                LockCam = !lockCam;
                habiaSalido = true;
            }

            if ((Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT) && habiaSalido))
            {
                LockCam = !lockCam;
                habiaSalido = false;
            }

            if (lockCam) { 
                leftrightRot -= -Input.XposRelative * RotationSpeed;
                updownRot -= Input.YposRelative * RotationSpeed;

            if (updownRot > 1.5)
                updownRot = 1.5f;
            if (updownRot < -1.5)
                updownRot = -1.5f;

            //Se actualiza matrix de rotacion, para no hacer este calculo cada vez y solo cuando en verdad es necesario.
            cameraRotation = TGCMatrix.RotationX(updownRot) * TGCMatrix.RotationY(leftrightRot);
            }

            if (lockCam)
                Cursor.Position = mouseCenter;

            //Calculamos la nueva posicion del ojo segun la rotacion actual de la camara.
            var cameraRotatedPositionEye = TGCVector3.TransformNormal(moveVector * elapsedTime, cameraRotation);
            positionEye += cameraRotatedPositionEye;

            //Calculamos el target de la camara, segun su direccion inicial y las rotaciones en screen space x,y.
            var cameraRotatedTarget = TGCVector3.TransformNormal(directionView, cameraRotation);
            var cameraFinalTarget = positionEye + cameraRotatedTarget;

            //Se calcula el nuevo vector de up producido por el movimiento del update.
            var cameraOriginalUpVector = DEFAULT_UP_VECTOR;
            var cameraRotatedUpVector = TGCVector3.TransformNormal(cameraOriginalUpVector, cameraRotation);

            base.SetCamera(positionEye, cameraFinalTarget, cameraRotatedUpVector);
        }

        public void setPosicion(TGCVector3 nuevaPos)
        {
            this.positionEye = nuevaPos;
        }

    }
}
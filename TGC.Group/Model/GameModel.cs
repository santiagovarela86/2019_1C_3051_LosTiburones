using Microsoft.DirectX.DirectInput;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Group.Model.Camara;
using TGC.Core.Terrain;
using TGC.Core.Sound;
using System.Collections.Generic;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer m�s ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {
        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }

        //Caja que se muestra en el ejemplo.
        private TGCBox Box { get; set; }

        //Mesh de TgcLogo.
       // private TgcMesh Mesh { get; set; }

        //Boleano para ver si dibujamos el boundingbox
        private bool BoundingBox { get; set; }

        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aqu� todo el c�digo de inicializaci�n: cargar modelos, texturas, estructuras de optimizaci�n, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        /// 
        //------------------------------------------------------
        private TgcFpsCamera camaraInterna;
        private TgcPlane piso, agua;
        private TgcMesh coralBrain, coral, shark, fish, pillarCoral, seaShell, spiralWireCoral, treeCoral, yellowFish;
        private TgcScene barco;
        private TgcSkyBox skybox;

        private TgcSimpleTerrain terreno;
        private float currentScaleXZ;
        private float currentScaleY;

        private TgcMp3Player musica;

        private List<TgcMesh> objetosEstaticos;

        //Constantes para velocidades de movimiento
        private const float ROTATION_SPEED = 50f;

        private const float MOVEMENT_SPEED = 50f;

        //Variable direccion de movimiento
        private float currentMoveDir = -1f;

        public TGCVector3 posInicialShark;

        public override void Init()
        {
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;

            //Textura de la carperta Media. Game.Default es un archivo de configuracion (Game.settings) util para poner cosas.
            //Pueden abrir el Game.settings que se ubica dentro de nuestro proyecto para configurar.
            var pathTexturaCaja = MediaDir + Game.Default.TexturaCaja;

            //Cargamos una textura, tener en cuenta que cargar una textura significa crear una copia en memoria.
            //Es importante cargar texturas en Init, si se hace en el render loop podemos tener grandes problemas si instanciamos muchas.
            var texture = TgcTexture.createTexture(pathTexturaCaja);

            //Creamos una caja 3D ubicada de dimensiones (5, 10, 5) y la textura como color.
            var size = new TGCVector3(5, 10, 5);
            //Construimos una caja seg�n los par�metros, por defecto la misma se crea con centro en el origen y se recomienda as� para facilitar las transformaciones.
            Box = TGCBox.fromSize(size, texture);
            //Posici�n donde quiero que este la caja, es com�n que se utilicen estructuras internas para las transformaciones.
            //Entonces actualizamos la posici�n l�gica, luego podemos utilizar esto en render para posicionar donde corresponda con transformaciones.
            Box.Position = new TGCVector3(-25, 0, 0);

            //Cargo el unico mesh que tiene la escena.
           // Mesh = new TgcSceneLoader().loadSceneFromFile(MediaDir + "LogoTGC-TgcScene.xml").Meshes[0];
            //Defino una escala en el modelo logico del mesh que es muy grande.
           // Mesh.Scale = new TGCVector3(0.5f, 0.5f, 0.5f);

            //Suelen utilizarse objetos que manejan el comportamiento de la camara.
            //Lo que en realidad necesitamos gr�ficamente es una matriz de View.
            //El framework maneja una c�mara est�tica, pero debe ser inicializada.
            //Posici�n de la camara.
            //var cameraPosition = new TGCVector3(0, 0, 125);
            //Quiero que la camara mire hacia el origen (0,0,0).
            //var lookAt = TGCVector3.Empty;
            //Configuro donde esta la posicion de la camara y hacia donde mira.
            //Camara.SetCamera(cameraPosition, lookAt);
            //Internamente el framework construye la matriz de view con estos dos vectores.
            //Luego en nuestro juego tendremos que crear una c�mara que cambie la matriz de view con variables como movimientos o animaciones de escenas.

            //-----------------------camara---------------------------------------------------


            camaraInterna = new TgcFpsCamera(new TGCVector3(5, 60, 0), 80f, 50f, Input);
            Camara = camaraInterna;
            //-------
            //-------------------------------pisos------------

            var aguaTextura = TgcTexture.createTexture(D3DDevice.Instance.Device, MediaDir + "Texturas\\agua20.jpg");
            agua = new TgcPlane(new TGCVector3(-5000, 0, -5000), new TGCVector3(10000, 0, 10000), TgcPlane.Orientations.XZplane, aguaTextura);



            var pisoTextura = TgcTexture.createTexture(D3DDevice.Instance.Device, MediaDir + "Texturas\\pasto.jpg");
            piso = new TgcPlane(new TGCVector3(-5000, -300, -5000), new TGCVector3(10000, 0, 10000), TgcPlane.Orientations.XZplane, pisoTextura);
            //------------------

            //-------Skybox
            skybox = new TgcSkyBox();
            skybox.Center = TGCVector3.Empty;
            skybox.Size = new TGCVector3(10000, 10000, 10000);

            var texturesPath = MediaDir + "Texturas\\SkyBox\\";

            skybox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "lostatseaday_up.jpg");
            skybox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "lostatseaday_dn.jpg");
            skybox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "lostatseaday_lf.jpg");
            skybox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "lostatseaday_rt.jpg");
            skybox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "lostatseaday_ft.jpg");
            skybox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "lostatseaday_bk.jpg");
            skybox.SkyEpsilon = 25f;
            skybox.Init();

            //----------------

            //--------- una carga basica de heighmap
            terreno = new TgcSimpleTerrain();
            var path = MediaDir + "Texturas\\Heighmaps\\heighmap.jpg";
            var textu = MediaDir + "Texturas\\Grass.jpg";
            currentScaleXZ = 20f;
            currentScaleY = 1.3f;
            terreno.loadHeightmap(path, currentScaleXZ, currentScaleY, new TGCVector3(0, -255, 0));
            terreno.loadTexture(textu);
            terreno.AlphaBlendEnable = true;

            //---------------

            //---------musica-----------
            musica = new TgcMp3Player();
            musica.FileName = MediaDir + "\\Music\\AbandonShip.mp3";
            musica.play(true);
            //----------



            //--------------objetos---------
            coral = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\coral-TgcScene.xml").Meshes[0];
            coral.Position = new TGCVector3(10, -300, 0);
            coral.Transform = TGCMatrix.Translation(coral.Position);

            shark = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\shark-TgcScene.xml").Meshes[0];
            shark.Position = new TGCVector3(-650, -100, 1000);
            shark.Transform = TGCMatrix.Translation(shark.Position);

            coralBrain = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\brain_coral-TgcScene.xml").Meshes[0];
            coralBrain.Position = new TGCVector3(-200, -300, 340);
            coralBrain.Transform = TGCMatrix.Translation(coralBrain.Position);

            barco = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\ship-TgcScene.xml");

            fish = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\fish-TgcScene.xml").Meshes[0];
            fish.Position = new TGCVector3(0, -200, 0);
            fish.Transform = TGCMatrix.Translation(fish.Position);

            pillarCoral = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\pillar_coral-TgcScene.xml").Meshes[0];
            pillarCoral.Position = new TGCVector3(0, -200, 40);
            pillarCoral.Transform = TGCMatrix.Translation(pillarCoral.Position);

            seaShell = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\sea_shell-TgcScene.xml").Meshes[0];
            seaShell.Position = new TGCVector3(500, -200, 40);
            seaShell.Transform = TGCMatrix.Translation(seaShell.Position);

            spiralWireCoral = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\spiral_wire_coral-TgcScene.xml").Meshes[0];
            spiralWireCoral.Position = new TGCVector3(-50, -300, 40);
            spiralWireCoral.Transform = TGCMatrix.Translation(spiralWireCoral.Position);

            treeCoral = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\tree_coral-TgcScene.xml").Meshes[0];
            treeCoral.Position = new TGCVector3(-70, -300, 200);
            treeCoral.Scale = new TGCVector3(10f, 10f, 10f);
            treeCoral.Transform = TGCMatrix.Scaling(treeCoral.Scale) * TGCMatrix.Translation(treeCoral.Position);

            yellowFish = new TgcSceneLoader().loadSceneFromFile(MediaDir + "\\Aquatic\\Meshes\\yellow_fish-TgcScene.xml").Meshes[0];
            yellowFish.Position = new TGCVector3(50, -200, -20);
            yellowFish.Transform = TGCMatrix.Translation(yellowFish.Position);


        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la l�gica de computo del modelo, as� como tambi�n verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();
            
            //Capturar Input teclado
            if (Input.keyPressed(Key.F))
            {
                BoundingBox = !BoundingBox;
            }
            /*
            //Capturar Input Mouse
            if (Input.buttonUp(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                //Como ejemplo podemos hacer un movimiento simple de la c�mara.
                //En este caso le sumamos un valor en Y
                Camara.SetCamera(Camara.Position + new TGCVector3(0, 10f, 0), Camara.LookAt);
                //Ver ejemplos de c�mara para otras operaciones posibles.

                //Si superamos cierto Y volvemos a la posici�n original.
                if (Camara.Position.Y > 300f)
                {
                    Camara.SetCamera(new TGCVector3(Camara.Position.X, 0f, Camara.Position.Z), Camara.LookAt);
                }
            }
            */

            //-----------movimientos-------------

            shark.Position += new TGCVector3(MOVEMENT_SPEED * ElapsedTime * currentMoveDir, 0, 0);

            if (!((posInicialShark.X + 500 > shark.Position.X) && (posInicialShark.X - 500 < shark.Position.X)))
            {
                currentMoveDir *= -1;
                shark.Rotation += new TGCVector3(0, FastMath.PI, 0);
            }

            shark.Transform = TGCMatrix.RotationYawPitchRoll(shark.Rotation.X, shark.Rotation.Y, shark.Rotation.Z) * TGCMatrix.Translation(shark.Position);
            //-----------

            //---------------

                       
            //-----Skybox
            skybox.Center = Camara.Position;
            //----------


            PostUpdate();
        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aqu� todo el c�digo referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>
        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones seg�n nuestra conveniencia.
            PreRender();

            //Dibuja un texto por pantalla
            DrawText.drawText("Con la tecla F se dibuja el bounding box.", 0, 20, Color.OrangeRed);
            DrawText.drawText("Con clic izquierdo subimos la camara [Actual]: " + TGCVector3.PrintVector3(Camara.Position), 0, 30, Color.OrangeRed);

            //Siempre antes de renderizar el modelo necesitamos actualizar la matriz de transformacion.
            //Debemos recordar el orden en cual debemos multiplicar las matrices, en caso de tener modelos jer�rquicos, tenemos control total.
            Box.Transform = TGCMatrix.Scaling(Box.Scale) * TGCMatrix.RotationYawPitchRoll(Box.Rotation.Y, Box.Rotation.X, Box.Rotation.Z) * TGCMatrix.Translation(Box.Position);
            //A modo ejemplo realizamos toda las multiplicaciones, pero aqu� solo nos hacia falta la traslaci�n.
            //Finalmente invocamos al render de la caja
            Box.Render();

            //Cuando tenemos modelos mesh podemos utilizar un m�todo que hace la matriz de transformaci�n est�ndar.
            //Es �til cuando tenemos transformaciones simples, pero OJO cuando tenemos transformaciones jer�rquicas o complicadas.
           // Mesh.UpdateMeshTransform();
            //Render del mesh
           // Mesh.Render();

            //Render de BoundingBox, muy �til para debug de colisiones.
            if (BoundingBox)
            {
                Box.BoundingBox.Render();
               // Mesh.BoundingBox.Render();
            }


            //--------skybox---------
            skybox.Render();
            //---------------
            //----heighmap
            terreno.Render();

            //------------------------------------
            agua.Render();
            piso.Render();
            coral.Render();
            shark.Render();
            coralBrain.Render();
            barco.RenderAll();
            fish.Render();
            pillarCoral.Render();
            seaShell.Render();
            spiralWireCoral.Render();
            treeCoral.Render();
            yellowFish.Render();

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        /// <summary>
        ///     Se llama cuando termina la ejecuci�n del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gr�ficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            //Dispose de la caja.
            Box.Dispose();
            //Dispose del mesh.
            //Mesh.Dispose();

            //------------------------
            skybox.Dispose();
            terreno.Dispose();
            agua.Dispose();
            piso.Dispose();
            coral.Dispose();
            shark.Dispose();
            coralBrain.Dispose();
            barco.DisposeAll();
            fish.Dispose();
            pillarCoral.Dispose();
            seaShell.Render();
            spiralWireCoral.Render();
            treeCoral.Render();
            yellowFish.Render();

        }
    }
}
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXDataAnchor : Port, IControlledElement<VFXDataAnchorPresenter>, IEdgeConnectorListener
    {
        VisualElement m_ConnectorHighlight;

        VFXDataAnchorPresenter m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXDataAnchorPresenter controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }


        protected VFXDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, type)
        {
            AddToClassList("VFXDataAnchor");

            m_ConnectorHighlight = new VisualElement();

            m_ConnectorHighlight.style.positionType = PositionType.Absolute;
            m_ConnectorHighlight.style.positionTop = 0;
            m_ConnectorHighlight.style.positionLeft = 0;
            m_ConnectorHighlight.style.positionBottom = 0;
            m_ConnectorHighlight.style.positionRight = 0;
            m_ConnectorHighlight.pickingMode = PickingMode.Ignore;

            VisualElement connector = m_ConnectorBox as VisualElement;

            connector.Add(m_ConnectorHighlight);
        }

        protected override VisualElement CreateConnector()
        {
            return new VisualElement();
        }

        public static VFXDataAnchor Create(VFXDataAnchorPresenter presenter)
        {
            var anchor = new VFXDataAnchor(presenter.orientation, presenter.direction, presenter.portType);
            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = presenter;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        public enum IconType
        {
            plus,
            minus,
            simple
        }

        public static Texture2D GetTypeIcon(Type type, IconType iconType)
        {
            string suffix = "";
            switch (iconType)
            {
                case IconType.plus:
                    suffix = "_plus";
                    break;
                case IconType.minus:
                    suffix = "_minus";
                    break;
            }

            Texture2D result = Resources.Load<Texture2D>("VFX/" + type.Name + suffix);
            if (result == null)
                return Resources.Load<Texture2D>("VFX/Default" + suffix);
            return result;
        }

        const string AnchorColorProperty = "anchor-color";
        StyleValue<Color> m_AnchorColor;


        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);
        }

        IEnumerable<VFXDataEdge> GetAllEdges()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            foreach (var edgePresenter in controller.connections)
            {
                VFXDataEdge edge = view.GetDataEdgeByPresenter(edgePresenter as VFXDataEdgePresenter);
                if (edge != null)
                    yield return edge;
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            m_ConnectorText.text = "";

            VFXDataAnchorPresenter presenter = controller;

            if (presenter.connected)
                AddToClassList("connected");
            else
                RemoveFromClassList("connected");


            // update the css type of the class
            foreach (var cls in VFXTypeDefinition.GetTypeCSSClasses())
            {
                m_ConnectorBox.RemoveFromClassList(cls);
                RemoveFromClassList(cls);
            }

            string className = VFXTypeDefinition.GetTypeCSSClass(presenter.portType);
            AddToClassList(className);
            m_ConnectorBox.AddToClassList(className);

            AddToClassList("EdgeConnector");

            switch (presenter.direction)
            {
                case Direction.Input:
                    AddToClassList("Input");
                    break;
                case Direction.Output:
                    AddToClassList("Output");
                    break;
            }


            RemoveFromClassList("hidden");
            RemoveFromClassList("invisible");

            if (presenter.collapsed && !presenter.connected)
            {
                visible = false;

                AddToClassList("hidden");
                AddToClassList("invisible");
            }
            else if (!visible)
            {
                visible = true;
            }

            if (presenter.direction == Direction.Output)
                m_ConnectorText.text = presenter.name;
        }

        void IEdgeConnectorListener.OnDrop(GraphView graphView, Edge edge)
        {
            VFXView view = graphView as VFXView;
            VFXDataEdge dataEdge = edge as VFXDataEdge;
            VFXDataEdgePresenter edgePresenter = VFXDataEdgePresenter.CreateInstance<VFXDataEdgePresenter>();
            edgePresenter.Init(dataEdge.input.controller, dataEdge.output.controller);
            dataEdge.controller = edgePresenter;

            view.controller.AddElement(edgePresenter);
        }

        void IEdgeConnectorListener.OnDropOutsidePort(Edge edge, Vector2 position)
        {
            VFXDataAnchorPresenter presenter = controller;

            VFXSlot startSlot = presenter.model;

            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewPresenter viewPresenter = view.controller;


            VFXNodeUI endNode = null;
            foreach (var node in view.GetAllNodes().OfType<VFXNodeUI>())
            {
                if (node.worldBound.Contains(position))
                {
                    endNode = node;
                }
            }

            if (endNode != null)
            {
                VFXSlotContainerPresenter nodePresenter = endNode.controller.slotContainerPresenter;

                var compatibleAnchors = nodePresenter.viewPresenter.GetCompatiblePorts(controller, null);

                if (nodePresenter != null)
                {
                    IVFXSlotContainer slotContainer = nodePresenter.slotContainer;
                    if (presenter.direction == Direction.Input)
                    {
                        foreach (var outputSlot in slotContainer.outputSlots)
                        {
                            var endPresenter = nodePresenter.outputPorts.First(t => t.model == outputSlot);
                            if (compatibleAnchors.Contains(endPresenter))
                            {
                                startSlot.Link(outputSlot);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var inputSlot in slotContainer.inputSlots)
                        {
                            var endPresenter = nodePresenter.inputPorts.First(t => t.model == inputSlot);
                            if (compatibleAnchors.Contains(endPresenter))
                            {
                                inputSlot.Link(startSlot);
                                break;
                            }
                        }
                    }
                }
            }
            else if (presenter.direction == Direction.Input && Event.current.modifiers == EventModifiers.Alt)
            {
                VFXModelDescriptorParameters parameterDesc = VFXLibrary.GetParameters().FirstOrDefault(t => t.name == presenter.portType.UserFriendlyName());
                if (parameterDesc != null)
                {
                    VFXParameter parameter = viewPresenter.AddVFXParameter(view.contentViewContainer.GlobalToBound(position) - new Vector2(360, 0), parameterDesc);
                    startSlot.Link(parameter.outputSlots[0]);
                }
            }
            else
            {
                VFXFilterWindow.Show(Event.current.mousePosition, new VFXNodeProvider(AddLinkedNode, ProviderFilter, new Type[] { typeof(VFXOperator), typeof(VFXParameter) }));
            }
        }

        bool ProviderFilter(VFXNodeProvider.Descriptor d)
        {
            var mySlot = controller.model;

            VFXModelDescriptor desc = d.modelDescriptor as VFXModelDescriptor;
            if (desc == null)
                return false;

            IVFXSlotContainer container = desc.model as IVFXSlotContainer;
            if (container == null)
            {
                return false;
            }

            var getSlots = direction == Direction.Input ? (System.Func<int, VFXSlot> )container.GetOutputSlot : (System.Func<int, VFXSlot> )container.GetInputSlot;

            int count = direction == Direction.Input ? container.GetNbOutputSlots() : container.GetNbInputSlots();


            bool oneFound = false;
            for (int i = 0; i < count; ++i)
            {
                VFXSlot slot = getSlots(i);

                if (slot.CanLink(mySlot))
                {
                    oneFound = true;
                    break;
                }
            }

            return oneFound;
        }

        void AddLinkedNode(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            var mySlot = controller.model;
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view == null) return;
            Vector2 tPos = view.ChangeCoordinatesTo(view.contentViewContainer, mPos);

            VFXModelDescriptor desc = d.modelDescriptor as VFXModelDescriptor;

            IVFXSlotContainer  result = view.AddNode(d, mPos) as IVFXSlotContainer;

            if (result == null)
                return;


            var getSlots = direction == Direction.Input ? (System.Func<int, VFXSlot>)result.GetOutputSlot : (System.Func<int, VFXSlot>)result.GetInputSlot;

            int count = direction == Direction.Input ? result.GetNbOutputSlots() : result.GetNbInputSlots();

            for (int i = 0; i < count; ++i)
            {
                VFXSlot slot = getSlots(i);

                if (slot.CanLink(mySlot))
                {
                    slot.Link(mySlot);
                    return;
                }
            }
        }

        public override void DoRepaint()
        {
            base.DoRepaint();
        }
    }
}

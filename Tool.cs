using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessingActivity
{
    public enum ToolEvent
    {
        Hover,
        Blur, // When moving the mouse but not hovering
        Click,
        ClickHover, // Hovering while mouse is down, basically Dragging
        EndClickHover, // Ending Drag
    }

    internal class Tool
    {
        public int index = -1; // Used for setting the tab control for tool options
        public string name;
        public Dictionary<ToolEvent, Action<Graphics>> events = new Dictionary<ToolEvent, Action<Graphics>>();

        public Tool(int index, string name, Dictionary<ToolEvent, Action<Graphics>> events)
        {
            this.name = name;
            this.index = index;
            this.events = events;
        }

        public Tool(int index, string name)
        {
            this.name = name;
            this.index = index;
        }

        public bool Has(ToolEvent ev){
            return events.ContainsKey(ev);
        }

        public void RunEvent(ToolEvent ev, Graphics g)
        {
            events[ev](g);
        }

        public void RunIfExists(ToolEvent ev, Graphics g)
        {
            if (Has(ev)) RunEvent(ev, g);
        }

    }
}

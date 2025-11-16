<script lang="ts">
import { defineComponent, h, type DefineComponent, PropType } from 'vue';
import { Line } from 'vue-chartjs';
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  LineElement,
  LinearScale,
  CategoryScale,
  PointElement,
  Filler,
  type ChartData,
  type ChartOptions
} from 'chart.js';
import streamingPlugin from '@nckrtl/chartjs-plugin-streaming';
import 'chartjs-adapter-dayjs-4';

ChartJS.register(
  Title,
  Tooltip,
  Legend,
  LineElement,
  LinearScale,
  CategoryScale,
  PointElement,
  Filler,
  streamingPlugin
);

const LineChartComponent = Line as DefineComponent<Record<string, unknown>, Record<string, unknown>, unknown>;

ChartJS.defaults.color = '#c0c0c0';
ChartJS.defaults.font = {
  family: "'Exo', sans-serif",
  size: 11,
  weight: '500'
};

export default defineComponent({
  name: 'StreamChart',
  components: {
    LineChart: LineChartComponent
  },
  props: {
    data: {
      type: Object as PropType<ChartData<'line'>>,
      required: true
    },
    options: {
      type: Object as PropType<ChartOptions<'line'>>,
      default: () => ({})
    }
  },
  setup(props) {
    return () =>
      h(LineChartComponent, {
        data: props.data,
        options: props.options
      });
  }
});
</script>

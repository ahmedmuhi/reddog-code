<script lang="ts">
import { defineComponent, h, PropType } from 'vue';
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

ChartJS.register(Title, Tooltip, Legend, LineElement, LinearScale, CategoryScale, PointElement, Filler);

ChartJS.defaults.color = '#c0c0c0';
ChartJS.defaults.font = {
  family: "'Exo', sans-serif",
  size: 11,
  weight: '500'
};

export default defineComponent({
  name: 'StreamChart',
  components: {
    LineChart: Line
  },
  props: {
    chartData: {
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
      h(Line, {
        data: props.chartData,
        options: props.options
      });
  }
});
</script>

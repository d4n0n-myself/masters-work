<template>
  <div>
    <h2>Список датасетов:</h2>
    <ul>
      <li v-for="item in datasets" :key="item">{{ item }}</li>
    </ul>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  data() {
    return {
      datasets: [],
    };
  },
  mounted() {
    this.fetchData();
  },
  methods: {
    fetchData() {
      axios.get('http://localhost:5050/Results/ListDatasets')
        .then(response => {
          this.datasets = response.data;
        })
        .catch(error => {
          console.error(error);
        });
    },
  },
};
</script>

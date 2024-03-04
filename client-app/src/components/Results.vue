<template>
  <h2>Список результатов для 39b67cf9-e5fa-40b6-9558-956d5f9fb325:</h2>
  <div v-for="(values, key) in result" :key="key">
    {{ key }}
    <div v-for="value in values" :key="value">
      <div v-for="i in value" :key="value">
        {{ i }}
      </div>
    </div>
  </div>
    
</template>

<script>
import axios from 'axios';

export default {
  data() {
    return {
      result: {},
    };
  },
  mounted() {
    this.fetchData();
  },
  methods: {
    fetchData() {
      const id = '39b67cf9-e5fa-40b6-9558-956d5f9fb325';
      const url = `http://localhost:5050/Results/ListFiles?id=${id}`;

      axios.get(url)
        .then(response => {
          this.result = response.data;
        })
        .catch(error => {
          console.error(error);
        });
    },
  },
};
</script>

<template>
  <h2>Список результатов для 2966c007-8511-457d-8546-16dffecf574f:</h2>
  <div v-for="(values, key) in result" :key="key">
    <details>
      <summary :id="key">{{ key }} </summary>
      <div style="margin: 1%"></div>
      <table style="table-layout: fixed; width: 100%;border-collapse: collapse">
        <tr>
          <td style="border-color: var(--color-border); border-style: solid; margin: 0; padding: 5px;text-align: center"
              :colspan="14">
            Dataset
          </td>
          <td style="border-color: var(--color-border); border-style: solid; margin: 0; padding: 5px;text-align: center"
              :colspan="1">
            Result
          </td>
        </tr>
        <tr v-for="value in values" :key="value">
          <td style="border-color: var(--color-border); border-style: solid; margin: 0; padding: 5px"
              v-for="i in value" :key="value"> {{ i }}
          </td>
        </tr>
      </table>
    </details>
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
      const id = '2966c007-8511-457d-8546-16dffecf574f';
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

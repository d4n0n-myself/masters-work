<template>
  <h2>Список результатов для  {{ this.$route.params.id }}:</h2>
  <div v-for="(values, key) in result" :key="key">
    <details>
      <summary :id="key"> {{ values.dto.filename }} </summary>
      Best model: {{ values.dto.best_model }} <br>
      Accuracy: {{ values.dto.accuracy }} <br>
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
        <tr v-for="value in values.value" :key="value">
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
  created() {
    this.fetchData(this.$route.params.id);
  },
  methods: {
    fetchData(id) {
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

import { useFormik } from "formik";
import * as Yup from "yup";
import FormikInput from "@/FormikInput.jsx";
import { useState } from "react";

const validate = (field, value, context) =>
  fetch("/api/validate", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      field,
      value,
    }),
  })
    .then((response) => response.json())
    .then((result) => {
      if (result.valid) {
        return true;
      } else {
        return context.createError({
          message: result.message,
        });
      }
    });

const AIInputForm = ({ onSubmit }) => {
  const formik = useFormik({
    initialValues: {
      name: "",
      age: 0,
    },
    validationSchema: Yup.object({
      name: Yup.string().test("name-language-rule", function (value, context) {
        return validate("name", value, context);
      }),
      age: Yup.number().test("age-language-rule", function (value, context) {
        return validate("age", value, context);
      }),
    }),
    onSubmit: (values) => {
      onSubmit(values);
    },
  });

  const [rules, setRules] = useState({ _entity: "", name: "", age: "" });
  const ruleChanged = (field) => (e) => {
    setRules((r) => ({ ...r, [field]: e.target.value }));
  };

  const [configRuleActive, setConfigRuleActive] = useState({});
  const configureRule = (field, rule) => {
    setConfigRuleActive((c) => ({ ...c, [field]: true }));
    fetch("http://localhost:5086/api/configureRule", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        field,
        ruleText: rule,
      }),
    }).then(() => {
      setConfigRuleActive((c) => ({ ...c, [field]: false }));
    });
  };

  return (
    <form
      onSubmit={formik.handleSubmit}
      className="bg-red-50 rounded-lg p-8 flex flex-col mb-2 gap-2"
    >
      <div className="flex flex-row gap-2 mb-4">
        <h2 className="font-bold text-xl mb-4">Formik input, AI validation</h2>
        <div className="flex flex-row bg-red-200 rounded flex-grow p-1 gap-1">
          <textarea
            className="flex-grow"
            onChange={ruleChanged("_entity")}
            value={rules._entity}
          />
          <button
            className="bg-red-300 rounded px-2 disabled:bg-gray-300"
            type="button"
            onClick={() => configureRule("_entity", rules._entity)}
            disabled={configRuleActive._entity}
          >
            Set Entity Rule
          </button>
        </div>
      </div>
      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          field="name"
          label="Name"
          vertical={true}
        />
        <div className="flex flex-row bg-red-200 rounded flex-grow p-1 gap-1">
          <textarea
            className="flex-grow"
            onChange={ruleChanged("name")}
            value={rules.name}
          />
          <button
            className="bg-red-300 rounded px-2 disabled:bg-gray-300"
            type="button"
            onClick={() => configureRule("name", rules.name)}
            disabled={configRuleActive.name}
          >
            Set Rule
          </button>
        </div>
      </div>

      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          type="number"
          field="age"
          label="Age"
          vertical={true}
        />
        <div className="flex flex-row bg-red-200 rounded flex-grow p-1 gap-1">
          <textarea
            className="flex-grow"
            onChange={ruleChanged("age")}
            value={rules.age}
          />
          <button
            className="bg-red-300 rounded px-2 disabled:bg-gray-300"
            type="button"
            onClick={() => configureRule("age", rules.age)}
            disabled={configRuleActive.age}
          >
            Set Rule
          </button>
        </div>
      </div>

      <button
        type="submit"
        className="ml-auto bg-green-600 text-white font-bold px-4 py-2 rounded"
      >
        Submit
      </button>
    </form>
  );
};

export default AIInputForm;

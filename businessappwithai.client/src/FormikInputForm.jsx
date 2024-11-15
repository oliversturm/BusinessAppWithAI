import { useFormik } from "formik";
import * as Yup from "yup";
import FormikInput from "@/FormikInput.jsx";

const FormikInputForm = ({ onSubmit }) => {
  const formik = useFormik({
    initialValues: {
      name: "",
      age: 0,
    },
    validationSchema: Yup.object({
      name: Yup.string()
        .required("Name is required")
        .test("test-string", 'String must be "correct"', function (value) {
          if (value !== "correct") {
            return false;
          }
          return true;
        }),
      age: Yup.number()
        .min(1, "Age must be at least 1")
        .required("Age is required"),
    }),
    onSubmit: (values) => {
      onSubmit(values);
    },
  });

  return (
    <form
      onSubmit={formik.handleSubmit}
      className="bg-blue-50 rounded-lg p-8 flex flex-col mb-2 gap-2"
    >
      <h2 className="font-bold text-xl mb-4">Formik input, yup validation</h2>
      <FormikInput formik={formik} field="name" label="Name" />

      <FormikInput formik={formik} type="number" field="age" label="Age" />

      <button
        type="submit"
        className="ml-auto bg-green-600 text-white font-bold px-4 py-2 rounded"
      >
        Submit
      </button>
    </form>
  );
};

export default FormikInputForm;

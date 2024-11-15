const FormikInput = ({ formik, field, type, label }) => (
  <div className="bg-blue-200 rounded px-2 py-1 mb-2 flex flex-row items-center">
    <label htmlFor={field} className="block w-20">
      {label}
    </label>
    <input
      id={field}
      name={field}
      type={type || "text"}
      onChange={formik.handleChange}
      onBlur={formik.handleBlur}
      value={formik.values[field]}
      className="rounded px-2"
    />
    {formik.touched[field] && formik.errors[field] ? (
      <div className="ml-2 text-xs font-bold text-red-600">
        {formik.errors[field]}
      </div>
    ) : null}
  </div>
);

export default FormikInput;

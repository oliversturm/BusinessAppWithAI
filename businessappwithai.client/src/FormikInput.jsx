const FormikInput = ({ formik, field, type, label, vertical = false }) => (
  <div className="bg-blue-200 rounded px-2 py-1 flex flex-row">
    <label htmlFor={field} className="w-20">
      {label}
    </label>
    <div className={`flex ${vertical ? "flex-col" : "flex-row items-center"}`}>
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
  </div>
);

export default FormikInput;

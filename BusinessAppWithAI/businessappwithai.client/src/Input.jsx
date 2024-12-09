import { useCallback } from "react";

const Input = ({ field, type, label, value, onChange, error }) => {
  const innerChange = useCallback(
    (e) => {
      onChange(e.target.value);
    },
    [onChange],
  );

  return (
    <div className="bg-blue-200 rounded px-2 py-1 flex flex-row items-center">
      <label htmlFor={field} className="block w-20">
        {label}
      </label>
      <input
        id={field}
        name={field}
        type={type || "text"}
        onChange={innerChange}
        value={value}
        className="rounded px-2"
      />
      {error ? (
        <div className="ml-2 text-xs font-bold text-red-600">{error}</div>
      ) : null}
    </div>
  );
};

export default Input;

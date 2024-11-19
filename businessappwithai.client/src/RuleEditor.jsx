const RuleEditor = ({ name, onChange, value }) => {
  return (
    <div className="flex flex-row bg-red-200 rounded flex-grow p-1 gap-1">
      <textarea
        className="flex-grow text-xs leading-none h-12"
        onChange={onChange(name)}
        value={value}
      />
    </div>
  );
};

export default RuleEditor;
